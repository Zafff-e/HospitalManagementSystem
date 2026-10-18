using Hms.Application.Appointments.Dtos;

namespace Hms.Application.Common.Interfaces;

public interface IOpdHubClient
{
    Task QueueUpdated(OpdQueueItemDto item);
    Task PatientCalled(OpdQueueItemDto item);
}
