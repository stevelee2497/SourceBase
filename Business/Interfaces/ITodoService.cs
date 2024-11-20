using Core.DTOs;

namespace Business.Interfaces
{
    public interface ITodoService
    {
        Task<IEnumerable<TodoItemDetailDto>> GetTodoItemsAsync();

        Task<TodoItemDetailDto> GetTodoAsync(Guid id);

        Task CreateTodoAsync(TodoItemDto todoItem);

        Task UpdateTodoAsync(Guid id, TodoItemDto todoItem);

        Task DeleteTodoAsync(Guid id);
    }
}
