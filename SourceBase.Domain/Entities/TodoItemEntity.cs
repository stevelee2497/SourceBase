using System.ComponentModel.DataAnnotations.Schema;

namespace SourceBase.Domain.Entities;

public class TodoItemEntity : BaseEntity
{
    public required string Title { get; set; }

    public DateOnly Date { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public ItemStatus Status { get; set; }

    public required Guid UserId { get; set; }
}

public enum ItemStatus
{
    Open,
    Completed,
    Archived
}