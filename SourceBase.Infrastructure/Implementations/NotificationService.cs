using Microsoft.AspNetCore.SignalR;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.Hubs;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class NotificationService(IDbContext dbContext, IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task CreateAsync(NotificationEntity notification, CancellationToken ct)
    {
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(ct);

        await hubContext.Clients.Group(notification.UserId.ToString()).SendAsync(notification.Event.ToString(), notification, ct);
    }
}
