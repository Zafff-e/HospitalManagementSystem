using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities.Clinical;

/// <summary>
/// Doctor profile linked to a user account.
/// </summary>
public class Doctor : HospitalScopedEntity
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string? SpecialtyBn { get; set; }
    public string BmdcRegNo { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FullNameBn { get; set; }
    public string? Qualification { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Doctor's weekly schedule template.
/// </summary>
public class DoctorSchedule : BaseEntity
{
    public Guid DoctorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotMinutes { get; set; } = 15;
    public int MaxPatientsPerSlot { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public Doctor Doctor { get; set; } = null!;
}

/// <summary>
/// Patient appointment.
/// </summary>
public class Appointment : HospitalScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public int? TokenNo { get; set; }
    public string? Reason { get; set; }
    public string? CancellationReason { get; set; }

    public Doctor Doctor { get; set; } = null!;
}

/// <summary>
/// OPD token for queue management.
/// </summary>
public class OpdToken : BaseEntity
{
    public Guid DoctorId { get; set; }
    public DateTime Date { get; set; }
    public int TokenNo { get; set; }
    public Guid PatientId { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

    public Doctor Doctor { get; set; } = null!;
}

/// <summary>
/// Clinical encounter (OPD/IPD/ER visit).
/// </summary>
public class Encounter : HospitalScopedEntity
{
    public Guid PatientId { get; set; }
    public EncounterType Type { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public EncounterStatus Status { get; set; } = EncounterStatus.Active;

    public Doctor Doctor { get; set; } = null!;
    public ICollection<VitalSign> Vitals { get; set; } = new List<VitalSign>();
    public ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<ClinicalNote> Notes { get; set; } = new List<ClinicalNote>();
}

/// <summary>
/// Patient vital signs recorded during an encounter.
/// </summary>
public class VitalSign : BaseEntity
{
    public Guid EncounterId { get; set; }
    public decimal? Temperature { get; set; }
    public int? Pulse { get; set; }
    public int? BpSystolic { get; set; }
    public int? BpDiastolic { get; set; }
    public int? SpO2 { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public int? RespiratoryRate { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Encounter Encounter { get; set; } = null!;
}

/// <summary>
/// Diagnosis recorded during an encounter.
/// </summary>
public class Diagnosis : BaseEntity
{
    public Guid EncounterId { get; set; }
    public string? IcdCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionBn { get; set; }
    public bool IsPrimary { get; set; }

    public Encounter Encounter { get; set; } = null!;
}

/// <summary>
/// Prescription issued during an encounter.
/// Immutable — clinical data is append-only.
/// </summary>
public class Prescription : BaseEntity
{
    public Guid EncounterId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public DateTime? FollowUpDate { get; set; }

    public Encounter Encounter { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

/// <summary>
/// Individual medicine item in a prescription.
/// </summary>
public class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; set; }
    public Guid? MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Dose { get; set; }
    public string? Frequency { get; set; }
    public int? DurationDays { get; set; }
    public string? Instructions { get; set; }
    public string? InstructionsBn { get; set; }

    public Prescription Prescription { get; set; } = null!;
}

/// <summary>
/// Clinical notes recorded during an encounter.
/// </summary>
public class ClinicalNote : BaseEntity
{
    public Guid EncounterId { get; set; }
    public string Type { get; set; } = "General"; // General, History, Examination, Plan
    public string Body { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }

    public Encounter Encounter { get; set; } = null!;
}
