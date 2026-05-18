namespace SourceBase.Api.Entities;

public class TodoItemEntity : BaseAuditableEntity
{
    public required string Title { get; set; }

    public DateOnly Date { get; set; }

    public TodoItemStatus Status { get; set; }

    public required Guid UserId { get; set; }
}

public enum TodoItemStatus
{
    Open,
    Completed,
    Archived
}