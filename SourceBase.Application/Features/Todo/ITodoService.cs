namespace SourceBase.Application.Features.Todo;

public interface ITodoService
{
    Task<IEnumerable<TodoItemDetailResponse>> GetTodosAsync();
    Task<TodoItemDetailResponse> GetTodoAsync(Guid id);
    Task CreateTodoAsync(CreateTodoRequest todoItem);
    Task UpdateTodoAsync(Guid id, CreateTodoRequest todoItem);
    Task DeleteTodoAsync(Guid id);
}
