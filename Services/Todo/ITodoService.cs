using Core.Entities;

namespace Services.Todo
{
    public interface ITodoService
    {
        IEnumerable<TodoItemEntity> Get();
    }
}
