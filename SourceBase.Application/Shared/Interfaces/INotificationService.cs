namespace SourceBase.Application.Shared.Interfaces;

public interface INotificationService
{
    Task CreateAsync(NotificationEntity notification, CancellationToken ct);
}
