using System.Text.Json.Serialization;

namespace SourceBase.Api.Entities;

public class TodoItemEntity : BaseAuditableEntity
{
    public required string Title { get; set; }

    public DateOnly Date { get; set; }

    public TodoItemStatus Status { get; set; }

    public required Guid UserId { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoItemStatus
{
    Open,
    Completed,
    Archived
}