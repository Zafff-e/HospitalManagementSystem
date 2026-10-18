using Hms.Application.Patients.Dtos;

namespace Hms.Application.Common.Interfaces;

public interface IPatientService
{
    Task<PatientResponse> CreatePatientAsync(Guid hospitalId, CreatePatientRequest request, CancellationToken cancellationToken = default);
    Task<PatientResponse> UpdatePatientAsync(Guid hospitalId, Guid patientId, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<PatientResponse?> GetPatientByIdAsync(Guid hospitalId, Guid patientId, CancellationToken cancellationToken = default);
    Task<PagedResult<PatientResponse>> SearchPatientsAsync(Guid hospitalId, PatientSearchQuery query, CancellationToken cancellationToken = default);
    Task<DuplicateWarningDto> CheckDuplicateAsync(Guid hospitalId, string phone, string? nidLast4, DateTime? dob, CancellationToken cancellationToken = default);
}
