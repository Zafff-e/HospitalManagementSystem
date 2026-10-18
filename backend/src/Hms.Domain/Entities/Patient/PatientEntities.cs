using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities.Patient;

/// <summary>
/// Patient entity with bilingual names, encrypted NID, and BD address hierarchy.
/// </summary>
public class PatientRecord : HospitalScopedEntity
{
    // MRN — unique within hospital, format: YYYY-NNNNNN
    public string MRN { get; set; } = string.Empty;

    // Names (EN + BN)
    public string? FirstNameEn { get; set; }
    public string? LastNameEn { get; set; }
    public string? FirstNameBn { get; set; }
    public string? LastNameBn { get; set; }

    // Demographics
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public BloodGroup BloodGroup { get; set; } = BloodGroup.Unknown;
    public MaritalStatus? MaritalStatus { get; set; }
    public string? Occupation { get; set; }
    public string? Religion { get; set; }

    // Contact
    public string PhonePrimary { get; set; } = string.Empty; // E.164
    public string? PhoneSecondary { get; set; }
    public string? Email { get; set; }

    // Identity (encrypted at rest)
    public string? NidEncrypted { get; set; }
    public string? NidLast4 { get; set; }
    public string? BirthRegNo { get; set; }

    // Address (BD hierarchy)
    public int? DivisionId { get; set; }
    public int? DistrictId { get; set; }
    public int? UpazilaId { get; set; }
    public string? AddressLine { get; set; }

    // Photo
    public string? PhotoPath { get; set; }

    // Computed display name
    public string DisplayName => 
        !string.IsNullOrEmpty(FirstNameEn) 
            ? $"{FirstNameEn} {LastNameEn}".Trim() 
            : $"{FirstNameBn} {LastNameBn}".Trim();
}

/// <summary>
/// Patient emergency contact.
/// </summary>
public class PatientEmergencyContact : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    public PatientRecord Patient { get; set; } = null!;
}
