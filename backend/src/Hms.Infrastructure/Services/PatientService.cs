using Microsoft.EntityFrameworkCore;
using Hms.Application.Common.Interfaces;
using Hms.Application.Patients.Dtos;
using Hms.Domain.Entities.Patient;
using Hms.Domain.Exceptions;
using Hms.Domain.ValueObjects;
using Hms.Infrastructure.Persistence;

namespace Hms.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly HmsDbContext _db;
    private readonly IMrnGenerator _mrnGenerator;
    private readonly IAuditService _auditService;

    public PatientService(HmsDbContext db, IMrnGenerator mrnGenerator, IAuditService auditService)
    {
        _db = db;
        _mrnGenerator = mrnGenerator;
        _auditService = auditService;
    }

    public async Task<DuplicateWarningDto> CheckDuplicateAsync(
        Guid hospitalId, 
        string phone, 
        string? nidLast4, 
        DateTime? dob, 
        CancellationToken cancellationToken = default)
    {
        // 1. Check primary phone match
        var matchingByPhone = await _db.Patients
            .Where(p => p.HospitalId == hospitalId && p.PhonePrimary == phone)
            .Select(p => new { p.MRN, p.FirstNameEn, p.LastNameEn, p.FirstNameBn, p.LastNameBn })
            .FirstOrDefaultAsync(cancellationToken);

        if (matchingByPhone != null)
        {
            var name = !string.IsNullOrEmpty(matchingByPhone.FirstNameEn)
                ? $"{matchingByPhone.FirstNameEn} {matchingByPhone.LastNameEn}".Trim()
                : $"{matchingByPhone.FirstNameBn} {matchingByPhone.LastNameBn}".Trim();

            return new DuplicateWarningDto(
                HasDuplicateWarning: true,
                Reason: "A patient with this phone number already exists.",
                ExistingMrn: matchingByPhone.MRN,
                ExistingPatientName: name
            );
        }

        // 2. Check NID Last 4 + Date of Birth match
        if (!string.IsNullOrWhiteSpace(nidLast4) && dob.HasValue)
        {
            var matchingByNidAndDob = await _db.Patients
                .Where(p => p.HospitalId == hospitalId && p.NidLast4 == nidLast4 && p.DateOfBirth == dob.Value.Date)
                .Select(p => new { p.MRN, p.FirstNameEn, p.LastNameEn, p.FirstNameBn, p.LastNameBn })
                .FirstOrDefaultAsync(cancellationToken);

            if (matchingByNidAndDob != null)
            {
                var name = !string.IsNullOrEmpty(matchingByNidAndDob.FirstNameEn)
                    ? $"{matchingByNidAndDob.FirstNameEn} {matchingByNidAndDob.LastNameEn}".Trim()
                    : $"{matchingByNidAndDob.FirstNameBn} {matchingByNidAndDob.LastNameBn}".Trim();

                return new DuplicateWarningDto(
                    HasDuplicateWarning: true,
                    Reason: "A patient with matching NID (last 4 digits) and Date of Birth already exists.",
                    ExistingMrn: matchingByNidAndDob.MRN,
                    ExistingPatientName: name
                );
            }
        }

        return new DuplicateWarningDto(false, null, null, null);
    }

    public async Task<PatientResponse> CreatePatientAsync(Guid hospitalId, CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validation: At least one name required (EN or BN)
        var hasEnName = !string.IsNullOrWhiteSpace(request.FirstNameEn) || !string.IsNullOrWhiteSpace(request.LastNameEn);
        var hasBnName = !string.IsNullOrWhiteSpace(request.FirstNameBn) || !string.IsNullOrWhiteSpace(request.LastNameBn);
        if (!hasEnName && !hasBnName)
        {
            throw new DomainException("Patient name is required in either English or Bangla.");
        }

        // 2. Validation: DOB not in future
        if (request.DateOfBirth.HasValue && request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
        {
            throw new DomainException("Date of birth cannot be in the future.");
        }

        // 3. Validation: Phone normalisation to E.164
        var normalizedPhone = Phone.Create(request.PhonePrimary).Value;
        string? normalizedSecondary = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneSecondary))
        {
            normalizedSecondary = Phone.Create(request.PhoneSecondary).Value;
        }

        // 4. Validation: NID format & encryption
        string? nidLast4 = null;
        string? nidEncrypted = null;
        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            if (!NationalId.IsValidFormat(request.NationalId))
            {
                throw new DomainException("National ID must be 10, 13, or 17 digits.");
            }
            nidLast4 = request.NationalId.Trim()[^4..];
            nidEncrypted = $"ENC_AES_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.NationalId.Trim()))}";
        }

        // 5. Check duplicate warning (warn, do not block)
        var duplicateWarning = await CheckDuplicateAsync(hospitalId, normalizedPhone, nidLast4, request.DateOfBirth, cancellationToken);

        // 6. Generate MRN
        var mrn = await _mrnGenerator.GenerateMrnAsync(hospitalId, cancellationToken);

        var patient = new PatientRecord
        {
            HospitalId = hospitalId,
            MRN = mrn,
            FirstNameEn = request.FirstNameEn?.Trim(),
            LastNameEn = request.LastNameEn?.Trim(),
            FirstNameBn = request.FirstNameBn?.Trim(),
            LastNameBn = request.LastNameBn?.Trim(),
            DateOfBirth = request.DateOfBirth?.Date,
            Gender = request.Gender,
            BloodGroup = request.BloodGroup,
            PhonePrimary = normalizedPhone,
            PhoneSecondary = normalizedSecondary,
            Email = request.Email?.Trim(),
            NidEncrypted = nidEncrypted,
            NidLast4 = nidLast4,
            BirthRegNo = request.BirthRegNo?.Trim(),
            DivisionId = request.DivisionId,
            DistrictId = request.DistrictId,
            UpazilaId = request.UpazilaId,
            AddressLine = request.AddressLine?.Trim(),
            Occupation = request.Occupation?.Trim(),
            MaritalStatus = request.MaritalStatus,
            Religion = request.Religion?.Trim()
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);

        // Emergency contact if provided
        if (request.EmergencyContact != null)
        {
            var ec = new PatientEmergencyContact
            {
                PatientId = patient.Id,
                Name = request.EmergencyContact.Name,
                Relationship = request.EmergencyContact.Relationship,
                Phone = request.EmergencyContact.Phone
            };
            _db.PatientEmergencyContacts.Add(ec);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _auditService.LogAsync("CreatePatient", "PatientRecord", patient.Id.ToString(), newValues: new { patient.MRN, patient.PhonePrimary }, cancellationToken: cancellationToken);

        return MapToResponse(patient, duplicateWarning);
    }

    public async Task<PatientResponse> UpdatePatientAsync(Guid hospitalId, Guid patientId, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.HospitalId == hospitalId, cancellationToken);

        if (patient == null)
        {
            throw new DomainException($"Patient with ID '{patientId}' not found.");
        }

        var hasEnName = !string.IsNullOrWhiteSpace(request.FirstNameEn) || !string.IsNullOrWhiteSpace(request.LastNameEn);
        var hasBnName = !string.IsNullOrWhiteSpace(request.FirstNameBn) || !string.IsNullOrWhiteSpace(request.LastNameBn);
        if (!hasEnName && !hasBnName)
        {
            throw new DomainException("Patient name is required in either English or Bangla.");
        }

        if (request.DateOfBirth.HasValue && request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
        {
            throw new DomainException("Date of birth cannot be in the future.");
        }

        var normalizedPhone = Phone.Create(request.PhonePrimary).Value;
        string? normalizedSecondary = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneSecondary))
        {
            normalizedSecondary = Phone.Create(request.PhoneSecondary).Value;
        }

        string? nidLast4 = patient.NidLast4;
        string? nidEncrypted = patient.NidEncrypted;
        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            if (!NationalId.IsValidFormat(request.NationalId))
            {
                throw new DomainException("National ID must be 10, 13, or 17 digits.");
            }
            nidLast4 = request.NationalId.Trim()[^4..];
            nidEncrypted = $"ENC_AES_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.NationalId.Trim()))}";
        }

        patient.FirstNameEn = request.FirstNameEn?.Trim();
        patient.LastNameEn = request.LastNameEn?.Trim();
        patient.FirstNameBn = request.FirstNameBn?.Trim();
        patient.LastNameBn = request.LastNameBn?.Trim();
        patient.DateOfBirth = request.DateOfBirth?.Date;
        patient.Gender = request.Gender;
        patient.BloodGroup = request.BloodGroup;
        patient.PhonePrimary = normalizedPhone;
        patient.PhoneSecondary = normalizedSecondary;
        patient.Email = request.Email?.Trim();
        patient.NidEncrypted = nidEncrypted;
        patient.NidLast4 = nidLast4;
        patient.BirthRegNo = request.BirthRegNo?.Trim();
        patient.DivisionId = request.DivisionId;
        patient.DistrictId = request.DistrictId;
        patient.UpazilaId = request.UpazilaId;
        patient.AddressLine = request.AddressLine?.Trim();
        patient.Occupation = request.Occupation?.Trim();
        patient.MaritalStatus = request.MaritalStatus;
        patient.Religion = request.Religion?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("UpdatePatient", "PatientRecord", patient.Id.ToString(), cancellationToken: cancellationToken);

        return MapToResponse(patient);
    }

    public async Task<PatientResponse?> GetPatientByIdAsync(Guid hospitalId, Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.HospitalId == hospitalId, cancellationToken);

        return patient != null ? MapToResponse(patient) : null;
    }

    public async Task<PagedResult<PatientResponse>> SearchPatientsAsync(Guid hospitalId, PatientSearchQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _db.Patients
            .Where(p => p.HospitalId == hospitalId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Mrn))
        {
            var mrn = query.Mrn.Trim();
            dbQuery = dbQuery.Where(p => p.MRN == mrn);
        }

        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var phone = query.Phone.Trim();
            dbQuery = dbQuery.Where(p => p.PhonePrimary.Contains(phone));
        }

        if (!string.IsNullOrWhiteSpace(query.NidLast4))
        {
            var nid = query.NidLast4.Trim();
            dbQuery = dbQuery.Where(p => p.NidLast4 == nid);
        }

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var q = query.Query.Trim();
            dbQuery = dbQuery.Where(p => 
                (p.FirstNameEn != null && p.FirstNameEn.Contains(q)) ||
                (p.LastNameEn != null && p.LastNameEn.Contains(q)) ||
                (p.FirstNameBn != null && p.FirstNameBn.Contains(q)) ||
                (p.LastNameBn != null && p.LastNameBn.Contains(q)) ||
                p.MRN.Contains(q) ||
                p.PhonePrimary.Contains(q));
        }

        var total = await dbQuery.CountAsync(cancellationToken);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 25 : query.PageSize;

        var items = await dbQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(p => MapToResponse(p)).ToList();

        return new PagedResult<PatientResponse>(mapped, total, page, pageSize);
    }

    private static PatientResponse MapToResponse(PatientRecord p, DuplicateWarningDto? warning = null)
    {
        int? ageYears = null;
        if (p.DateOfBirth.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - p.DateOfBirth.Value.Year;
            if (p.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
            ageYears = age;
        }

        return new PatientResponse(
            p.Id,
            p.HospitalId,
            p.MRN,
            p.FirstNameEn,
            p.LastNameEn,
            p.FirstNameBn,
            p.LastNameBn,
            p.DisplayName,
            p.DateOfBirth,
            ageYears,
            p.Gender,
            p.BloodGroup,
            p.PhonePrimary,
            p.PhoneSecondary,
            p.Email,
            p.NidLast4,
            p.BirthRegNo,
            p.DivisionId,
            null, // Navigation can be resolved in full views
            p.DistrictId,
            null,
            p.UpazilaId,
            null,
            p.AddressLine,
            p.Occupation,
            p.MaritalStatus,
            p.Religion,
            p.CreatedAt,
            warning
        );
    }
}
