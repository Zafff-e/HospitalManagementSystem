using Hms.Application.Emr.Dtos;

namespace Hms.Application.Common.Interfaces;

public interface IEmrService
{
    Task<EncounterResponse> StartEncounterAsync(Guid hospitalId, Guid doctorUserId, CreateEncounterRequest request, CancellationToken ct = default);
    Task<EncounterResponse> GetEncounterAsync(Guid encounterId, CancellationToken ct = default);
    Task<VitalSignDto> RecordVitalsAsync(Guid encounterId, Guid recordedByUserId, RecordVitalsRequest request, CancellationToken ct = default);
    Task<DiagnosisDto> AddDiagnosisAsync(Guid encounterId, AddDiagnosisRequest request, CancellationToken ct = default);
    Task<ClinicalNoteDto> AddNoteAsync(Guid encounterId, Guid authorUserId, AddClinicalNoteRequest request, CancellationToken ct = default);
    Task<PrescriptionResponse> CreatePrescriptionAsync(Guid hospitalId, Guid doctorUserId, CreatePrescriptionRequest request, CancellationToken ct = default);
    Task<byte[]> GeneratePrescriptionPdfAsync(Guid prescriptionId, CancellationToken ct = default);
    Task<IReadOnlyList<Icd10CatalogueItemDto>> SearchIcd10Async(string? query, CancellationToken ct = default);
    Task<IReadOnlyList<MedicineCatalogueItemDto>> SearchMedicinesAsync(string? query, CancellationToken ct = default);
}
