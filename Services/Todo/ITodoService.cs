using Core.DTOs;

namespace Services.Todo
{
    public interface ITodoService
    {
        Task<IEnumerable<TodoItemDetailDto>> GetTodoItems();

        Task<TodoItemDetailDto> GetTodo(Guid id);

        Task CreateTodo(TodoItemDto todoItem);

        Task UpdateTodo(Guid id, TodoItemDto todoItem);

        Task DeleteTodo(Guid id);
    }
}
