using Microsoft.AspNetCore.SignalR;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.Hubs;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class MachineCommandService(IHubContext<NotificationHub> hubContext) : IMachineCommandService
{
    public async Task SendCommandAsync(Guid userId, Guid machineId, MachineCommandType type, CancellationToken ct)
    {
        await hubContext.Clients.Group(userId.ToString()).SendAsync("MachineCommandEvent", type.ToString(), ct);
    }
}
