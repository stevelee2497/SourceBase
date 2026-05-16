using System.ComponentModel.DataAnnotations.Schema;

namespace SourceBase.Api.Entities;

public class TodoItemEntity : BaseEntity
{
    public required string Title { get; set; }

    public DateOnly Date { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public TodoItemStatus Status { get; set; }

    public required Guid UserId { get; set; }
}

public enum TodoItemStatus
{
    Open,
    Completed,
    Archived
}