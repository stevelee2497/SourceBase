namespace SourceBase.Domain.Entities;

public class TodoListEntity : BaseAuditableEntity
{
    public required string Name { get; set; }

    public required Guid UserId { get; set; }

    public ICollection<TodoItemEntity> Items { get; set; } = [];
}
