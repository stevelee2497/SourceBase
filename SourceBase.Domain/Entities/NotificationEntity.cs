namespace SourceBase.Domain.Entities;

public class NotificationEntity : BaseAuditableEntity
{
    public required Guid UserId { get; set; }

    public required NotificationEvent Event { get; set; }

    public required string Title { get; set; }

    public required string Message { get; set; }

    public required string Data { get; set; }

    public bool IsRead { get; set; }
}

public enum NotificationEvent
{
    GlobalNotificationEvent,
    TodoUpdatedEvent,
    TodoCreatedEvent
}