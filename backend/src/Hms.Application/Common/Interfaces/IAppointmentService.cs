using Hms.Application.Appointments.Dtos;

namespace Hms.Application.Common.Interfaces;

public interface IAppointmentService
{
    Task<IReadOnlyList<DoctorDto>> GetDoctorsAsync(Guid hospitalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimeSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default);
    Task<AppointmentResponse> BookAppointmentAsync(Guid hospitalId, BookAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<CheckInResponse> CheckInAsync(Guid hospitalId, Guid appointmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpdQueueItemDto>> GetDoctorQueueAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentResponse>> GetAppointmentsAsync(Guid hospitalId, DateTime? date, Guid? doctorId, Guid? patientId, CancellationToken cancellationToken = default);
}
