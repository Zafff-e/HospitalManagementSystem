using Hms.Domain.Enums;

namespace Hms.Application.Appointments.Dtos;

public record DoctorDto(
    Guid Id,
    Guid UserId,
    Guid DepartmentId,
    string FullName,
    string? FullNameBn,
    string Specialty,
    string? SpecialtyBn,
    string BmdcRegNo,
    decimal ConsultationFee,
    string? Qualification
);

public record DoctorScheduleDto(
    Guid Id,
    Guid DoctorId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int SlotMinutes,
    int MaxPatientsPerSlot
);

public record TimeSlotDto(
    DateTime SlotTime,
    bool IsAvailable,
    int BookedCount,
    int MaxCapacity
);

public record BookAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    DateTime ScheduledAt,
    string? Reason = null
);

public record AppointmentResponse(
    Guid Id,
    Guid HospitalId,
    Guid PatientId,
    string PatientName,
    string PatientMrn,
    string PatientPhone,
    Guid DoctorId,
    string DoctorName,
    string Specialty,
    DateTime ScheduledAt,
    AppointmentStatus Status,
    int? TokenNo,
    string? Reason,
    DateTime CreatedAt
);

public record CheckInResponse(
    Guid AppointmentId,
    int TokenNo,
    DateTime Date,
    string DoctorName,
    string PatientName,
    string PatientMrn
);

public record OpdQueueItemDto(
    Guid AppointmentId,
    int TokenNo,
    Guid PatientId,
    string PatientName,
    string PatientMrn,
    int? AgeYears,
    Gender Gender,
    DateTime ScheduledAt,
    AppointmentStatus Status
);
