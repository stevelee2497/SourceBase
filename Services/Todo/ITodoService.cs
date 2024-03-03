using Core.DTOs;

namespace Services.Todo
{
    public interface ITodoService
    {
        IEnumerable<TodoItemDetailDto> GetAll();

        TodoItemDetailDto Get(Guid id);

        void Save(TodoItemDto todoItem);

        void Update(Guid id, TodoItemDto todoItem);

        void Delete(Guid id);
    }
}
