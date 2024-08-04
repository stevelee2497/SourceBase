using Core.DTOs;

namespace Services.Todo
{
    public interface ITodoService
    {
        IEnumerable<TodoItemDetailDto> GetTodoItems();

        TodoItemDetailDto GetTodo(Guid id);

        Task CreateTodo(TodoItemDto todoItem);

        Task UpdateTodo(Guid id, TodoItemDto todoItem);

        Task DeleteTodo(Guid id);
    }
}
