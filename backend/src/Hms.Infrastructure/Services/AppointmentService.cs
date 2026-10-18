using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Hms.Application.Appointments.Dtos;
using Hms.Application.Common.Interfaces;
using Hms.Domain.Entities.Clinical;
using Hms.Domain.Enums;
using Hms.Domain.Exceptions;
using Hms.Infrastructure.Hubs;
using Hms.Infrastructure.Persistence;

namespace Hms.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly HmsDbContext _db;
    private readonly IAuditService _auditService;
    private readonly IHubContext<OpdHub, IOpdHubClient> _hubContext;

    // Slot locking dictionary to prevent concurrent overbooking race conditions
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _slotLocks = new();

    public AppointmentService(
        HmsDbContext db,
        IAuditService auditService,
        IHubContext<OpdHub, IOpdHubClient> hubContext)
    {
        _db = db;
        _auditService = auditService;
        _hubContext = hubContext;
    }

    private static readonly HashSet<(int Month, int Day)> _bdHolidays = new()
    {
        (2, 21), // Shaheed Day / International Mother Language Day
        (3, 26), // Independence Day
        (12, 16) // Victory Day
    };

    private static bool IsHoliday(DateTime date)
    {
        return _bdHolidays.Contains((date.Month, date.Day));
    }

    public async Task<IReadOnlyList<DoctorDto>> GetDoctorsAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        var doctors = await _db.Doctors
            .Where(d => d.HospitalId == hospitalId && d.IsActive)
            .OrderBy(d => d.FullName)
            .ToListAsync(cancellationToken);

        return doctors.Select(d => new DoctorDto(
            d.Id,
            d.UserId,
            d.DepartmentId,
            d.FullName,
            d.FullNameBn,
            d.Specialty,
            d.SpecialtyBn,
            d.BmdcRegNo,
            d.ConsultationFee,
            d.Qualification
        )).ToList();
    }

    public async Task<IReadOnlyList<TimeSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        if (IsHoliday(date))
        {
            return Array.Empty<TimeSlotDto>();
        }

        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == date.DayOfWeek && s.IsActive, cancellationToken);

        if (schedule == null)
        {
            return Array.Empty<TimeSlotDto>();
        }

        var startDateTime = date.Date.Add(schedule.StartTime);
        var endDateTime = date.Date.Add(schedule.EndTime);

        // Fetch existing bookings for this doctor on this day
        var existingBookings = await _db.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.ScheduledAt >= startDateTime &&
                        a.ScheduledAt < endDateTime &&
                        a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.ScheduledAt)
            .Select(g => new { SlotTime = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SlotTime, x => x.Count, cancellationToken);

        var slots = new List<TimeSlotDto>();
        var current = startDateTime;
        var slotDuration = TimeSpan.FromMinutes(schedule.SlotMinutes > 0 ? schedule.SlotMinutes : 15);
        var maxCapacity = schedule.MaxPatientsPerSlot > 0 ? schedule.MaxPatientsPerSlot : 1;

        while (current + slotDuration <= endDateTime)
        {
            existingBookings.TryGetValue(current, out var bookedCount);
            var isAvailable = bookedCount < maxCapacity;
            slots.Add(new TimeSlotDto(current, isAvailable, bookedCount, maxCapacity));
            current += slotDuration;
        }

        return slots;
    }

    public async Task<AppointmentResponse> BookAppointmentAsync(Guid hospitalId, BookAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.HospitalId == hospitalId && d.IsActive, cancellationToken);

        if (doctor == null)
        {
            throw new DomainException($"Doctor with ID '{request.DoctorId}' not found or inactive.");
        }

        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.HospitalId == hospitalId, cancellationToken);

        if (patient == null)
        {
            throw new DomainException($"Patient with ID '{request.PatientId}' not found.");
        }

        if (IsHoliday(request.ScheduledAt))
        {
            throw new DomainException("Hospital is closed for OPD consultations on national holidays.");
        }

        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == request.DoctorId && s.DayOfWeek == request.ScheduledAt.DayOfWeek && s.IsActive, cancellationToken);

        if (schedule == null)
        {
            throw new DomainException($"Doctor {doctor.FullName} has no consultation schedule on {request.ScheduledAt.DayOfWeek}.");
        }

        var slotTime = request.ScheduledAt;
        var lockKey = $"{request.DoctorId}_{slotTime:yyyyMMddHHmm}";
        var slotLock = _slotLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await slotLock.WaitAsync(cancellationToken);
        try
        {
            var maxCapacity = schedule.MaxPatientsPerSlot > 0 ? schedule.MaxPatientsPerSlot : 1;
            var currentBookingsCount = await _db.Appointments
                .CountAsync(a => a.DoctorId == request.DoctorId &&
                                 a.ScheduledAt == slotTime &&
                                 a.Status != AppointmentStatus.Cancelled, cancellationToken);

            if (currentBookingsCount >= maxCapacity)
            {
                throw new ConflictException($"The consultation slot at {slotTime:yyyy-MM-dd HH:mm} is already fully booked.");
            }

            var appointment = new Appointment
            {
                HospitalId = hospitalId,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                ScheduledAt = slotTime,
                Status = AppointmentStatus.Booked,
                Reason = request.Reason?.Trim()
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("BookAppointment", "Appointment", appointment.Id.ToString(),
                newValues: new { appointment.DoctorId, appointment.PatientId, appointment.ScheduledAt },
                cancellationToken: cancellationToken);

            return new AppointmentResponse(
                appointment.Id,
                appointment.HospitalId,
                patient.Id,
                patient.DisplayName,
                patient.MRN,
                patient.PhonePrimary,
                doctor.Id,
                doctor.FullName,
                doctor.Specialty,
                appointment.ScheduledAt,
                appointment.Status,
                appointment.TokenNo,
                appointment.Reason,
                appointment.CreatedAt
            );
        }
        finally
        {
            slotLock.Release();
        }
    }

    public async Task<CheckInResponse> CheckInAsync(Guid hospitalId, Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.HospitalId == hospitalId, cancellationToken);

        if (appointment == null)
        {
            throw new DomainException($"Appointment with ID '{appointmentId}' not found.");
        }

        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == appointment.PatientId && p.HospitalId == hospitalId, cancellationToken);

        if (patient == null)
        {
            throw new DomainException("Associated patient record not found.");
        }

        // Determine date in Asia/Dhaka time (UTC+6)
        var dhakaTimeZone = GetDhakaTimeZone();
        var todayDhaka = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, dhakaTimeZone).Date;

        if (appointment.Status == AppointmentStatus.CheckedIn && appointment.TokenNo.HasValue)
        {
            return new CheckInResponse(
                appointment.Id,
                appointment.TokenNo.Value,
                todayDhaka,
                appointment.Doctor.FullName,
                patient.DisplayName,
                patient.MRN
            );
        }

        // Daily sequential token calculation per doctor per day
        var maxToken = await _db.OpdTokens
            .Where(t => t.DoctorId == appointment.DoctorId && t.Date == todayDhaka)
            .MaxAsync(t => (int?)t.TokenNo, cancellationToken) ?? 0;

        var nextToken = maxToken + 1;

        var opdToken = new OpdToken
        {
            DoctorId = appointment.DoctorId,
            Date = todayDhaka,
            TokenNo = nextToken,
            PatientId = appointment.PatientId,
            Status = AppointmentStatus.CheckedIn
        };

        _db.OpdTokens.Add(opdToken);

        appointment.Status = AppointmentStatus.CheckedIn;
        appointment.TokenNo = nextToken;

        await _db.SaveChangesAsync(cancellationToken);

        int? patientAge = null;
        if (patient.DateOfBirth.HasValue)
        {
            var age = todayDhaka.Year - patient.DateOfBirth.Value.Year;
            if (patient.DateOfBirth.Value.Date > todayDhaka.AddYears(-age)) age--;
            patientAge = age;
        }

        var queueItem = new OpdQueueItemDto(
            appointment.Id,
            nextToken,
            patient.Id,
            patient.DisplayName,
            patient.MRN,
            patientAge,
            patient.Gender,
            appointment.ScheduledAt,
            appointment.Status
        );

        // Broadcast real-time queue update over SignalR to subscribers
        await _hubContext.Clients.Group($"doctor_{appointment.DoctorId}").QueueUpdated(queueItem);
        await _hubContext.Clients.All.QueueUpdated(queueItem);

        await _auditService.LogAsync("CheckInAppointment", "Appointment", appointment.Id.ToString(),
            newValues: new { TokenNo = nextToken, Date = todayDhaka },
            cancellationToken: cancellationToken);

        return new CheckInResponse(
            appointment.Id,
            nextToken,
            todayDhaka,
            appointment.Doctor.FullName,
            patient.DisplayName,
            patient.MRN
        );
    }

    public async Task<IReadOnlyList<OpdQueueItemDto>> GetDoctorQueueAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var targetDate = date.Date;

        var appointments = await _db.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.ScheduledAt.Date == targetDate &&
                        (a.Status == AppointmentStatus.CheckedIn || a.Status == AppointmentStatus.InConsult) &&
                        a.TokenNo.HasValue)
            .OrderBy(a => a.TokenNo)
            .ToListAsync(cancellationToken);

        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
        var patients = await _db.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return appointments.Select(a =>
        {
            patients.TryGetValue(a.PatientId, out var p);

            int? ageYears = null;
            if (p?.DateOfBirth.HasValue == true)
            {
                var today = DateTime.UtcNow.Date;
                var age = today.Year - p.DateOfBirth.Value.Year;
                if (p.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                ageYears = age;
            }

            return new OpdQueueItemDto(
                a.Id,
                a.TokenNo ?? 0,
                a.PatientId,
                p?.DisplayName ?? "Unknown Patient",
                p?.MRN ?? "",
                ageYears,
                p?.Gender ?? Gender.Other,
                a.ScheduledAt,
                a.Status
            );
        }).ToList();
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetAppointmentsAsync(
        Guid hospitalId,
        DateTime? date,
        Guid? doctorId,
        Guid? patientId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.HospitalId == hospitalId)
            .AsNoTracking();

        if (date.HasValue)
        {
            var d = date.Value.Date;
            query = query.Where(a => a.ScheduledAt.Date == d);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == doctorId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        var appointments = await query
            .OrderBy(a => a.ScheduledAt)
            .ToListAsync(cancellationToken);

        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
        var patients = await _db.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return appointments.Select(a =>
        {
            patients.TryGetValue(a.PatientId, out var p);
            return new AppointmentResponse(
                a.Id,
                a.HospitalId,
                a.PatientId,
                p?.DisplayName ?? "Patient",
                p?.MRN ?? "",
                p?.PhonePrimary ?? "",
                a.DoctorId,
                a.Doctor.FullName,
                a.Doctor.Specialty,
                a.ScheduledAt,
                a.Status,
                a.TokenNo,
                a.Reason,
                a.CreatedAt
            );
        }).ToList();
    }

    private static TimeZoneInfo GetDhakaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("BST", TimeSpan.FromHours(6), "Bangladesh Standard Time", "Bangladesh Standard Time");
            }
        }
    }
}
