using Microsoft.AspNetCore.SignalR;
using Hms.Application.Common.Interfaces;

namespace Hms.Infrastructure.Hubs;

public class OpdHub : Hub<IOpdHubClient>
{
    public async Task JoinDoctorQueue(string doctorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor_{doctorId}");
    }

    public async Task LeaveDoctorQueue(string doctorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"doctor_{doctorId}");
    }
}
