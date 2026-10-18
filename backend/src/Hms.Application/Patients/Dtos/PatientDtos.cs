using Hms.Domain.Enums;

namespace Hms.Application.Patients.Dtos;

public record CreatePatientRequest(
    string? FirstNameEn,
    string? LastNameEn,
    string? FirstNameBn,
    string? LastNameBn,
    DateTime? DateOfBirth,
    Gender Gender,
    BloodGroup BloodGroup,
    string PhonePrimary,
    string? PhoneSecondary,
    string? Email,
    string? NationalId,
    string? BirthRegNo,
    int? DivisionId,
    int? DistrictId,
    int? UpazilaId,
    string? AddressLine,
    string? Occupation,
    MaritalStatus? MaritalStatus,
    string? Religion,
    PatientEmergencyContactDto? EmergencyContact
);

public record UpdatePatientRequest(
    string? FirstNameEn,
    string? LastNameEn,
    string? FirstNameBn,
    string? LastNameBn,
    DateTime? DateOfBirth,
    Gender Gender,
    BloodGroup BloodGroup,
    string PhonePrimary,
    string? PhoneSecondary,
    string? Email,
    string? NationalId,
    string? BirthRegNo,
    int? DivisionId,
    int? DistrictId,
    int? UpazilaId,
    string? AddressLine,
    string? Occupation,
    MaritalStatus? MaritalStatus,
    string? Religion
);

public record PatientEmergencyContactDto(
    string Name,
    string Relationship,
    string Phone
);

public record DuplicateWarningDto(
    bool HasDuplicateWarning,
    string? Reason,
    string? ExistingMrn,
    string? ExistingPatientName
);

public record PatientResponse(
    Guid Id,
    Guid HospitalId,
    string MRN,
    string? FirstNameEn,
    string? LastNameEn,
    string? FirstNameBn,
    string? LastNameBn,
    string DisplayName,
    DateTime? DateOfBirth,
    int? AgeYears,
    Gender Gender,
    BloodGroup BloodGroup,
    string PhonePrimary,
    string? PhoneSecondary,
    string? Email,
    string? NidLast4,
    string? BirthRegNo,
    int? DivisionId,
    string? DivisionName,
    int? DistrictId,
    string? DistrictName,
    int? UpazilaId,
    string? UpazilaName,
    string? AddressLine,
    string? Occupation,
    MaritalStatus? MaritalStatus,
    string? Religion,
    DateTime CreatedAt,
    DuplicateWarningDto? DuplicateWarning = null
);

public class PatientSearchQuery
{
    public string? Query { get; set; }
    public string? Phone { get; set; }
    public string? NidLast4 { get; set; }
    public string? Mrn { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
