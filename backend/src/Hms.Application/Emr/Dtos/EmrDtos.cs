using Hms.Domain.Enums;

namespace Hms.Application.Emr.Dtos;

public record CreateEncounterRequest(
    Guid PatientId,
    Guid DoctorId,
    EncounterType Type = EncounterType.OPD
);

public record RecordVitalsRequest(
    decimal? Temperature,
    int? Pulse,
    int? BpSystolic,
    int? BpDiastolic,
    int? SpO2,
    decimal? Weight,
    decimal? Height,
    int? RespiratoryRate
);

public record VitalSignDto(
    Guid Id,
    Guid EncounterId,
    decimal? Temperature,
    int? Pulse,
    int? BpSystolic,
    int? BpDiastolic,
    string? BpString,
    int? SpO2,
    decimal? Weight,
    decimal? Height,
    decimal? Bmi,
    int? RespiratoryRate,
    DateTime RecordedAt
);

public record AddDiagnosisRequest(
    string? IcdCode,
    string Description,
    string? DescriptionBn,
    bool IsPrimary
);

public record DiagnosisDto(
    Guid Id,
    Guid EncounterId,
    string? IcdCode,
    string Description,
    string? DescriptionBn,
    bool IsPrimary
);

public record AddClinicalNoteRequest(
    string Type,
    string Body
);

public record ClinicalNoteDto(
    Guid Id,
    Guid EncounterId,
    string Type,
    string Body,
    Guid AuthorId,
    DateTime CreatedAt
);

public record PrescriptionItemRequest(
    Guid? MedicineId,
    string MedicineName,
    string? Dose,
    string? Frequency,
    int? DurationDays,
    string? Instructions,
    string? InstructionsBn
);

public record PrescriptionItemDto(
    Guid Id,
    Guid? MedicineId,
    string MedicineName,
    string? Dose,
    string? Frequency,
    int? DurationDays,
    string? Instructions,
    string? InstructionsBn
);

public record CreatePrescriptionRequest(
    Guid EncounterId,
    string? Notes,
    DateTime? FollowUpDate,
    List<PrescriptionItemRequest> Items
);

public record PrescriptionResponse(
    Guid Id,
    Guid EncounterId,
    Guid DoctorId,
    string DoctorName,
    string DoctorBmdc,
    string? DoctorQualification,
    string? DoctorSpecialty,
    Guid PatientId,
    string PatientName,
    string PatientNameBn,
    string PatientMrn,
    int? PatientAgeYears,
    string? PatientGender,
    decimal? PatientWeight,
    DateTime IssuedAt,
    string? Notes,
    DateTime? FollowUpDate,
    List<PrescriptionItemDto> Items
);

public record EncounterResponse(
    Guid Id,
    Guid HospitalId,
    Guid PatientId,
    string PatientMrn,
    string PatientName,
    string PatientNameBn,
    int? PatientAgeYears,
    string? PatientGender,
    Guid DoctorId,
    string DoctorName,
    string DoctorBmdc,
    string Specialty,
    string EncounterType,
    string Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    List<VitalSignDto> Vitals,
    List<DiagnosisDto> Diagnoses,
    List<ClinicalNoteDto> Notes,
    List<PrescriptionResponse> Prescriptions
);

public record Icd10CatalogueItemDto(
    string Code,
    string Description,
    string? DescriptionBn,
    string Category
);

public record MedicineCatalogueItemDto(
    Guid Id,
    string GenericName,
    string BrandName,
    string Strength,
    string Form,
    string Unit
);
