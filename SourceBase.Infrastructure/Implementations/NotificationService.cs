using Microsoft.AspNetCore.SignalR;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.Hubs;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class NotificationService(IDbContext dbContext, IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task CreateAsync(Guid userId, string title, string message, CancellationToken ct)
    {
        var notification = new NotificationEntity
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(ct);

        await hubContext.Clients.Group(userId.ToString()).SendAsync("GlobalNotificationEvent", new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            notification.CreatedOn,
        }, ct);
    }
}
