namespace SourceBase.Api.Shared.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid userId, string title, string message, CancellationToken ct);
}
