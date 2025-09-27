using Application.Features.Todo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/todos")]
public class TodoController(ITodoService todoService) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<TodoItemDetailResponse>> GetTodoItems()
    {
        return todoService.GetTodosAsync();
    }

    [HttpGet("{id}")]
    public Task<TodoItemDetailResponse> GetTodo(Guid id)
    {
        return todoService.GetTodoAsync(id);
    }

    [HttpPost]
    public Task CreateTodo(CreateTodoRequest todo)
    {
        return todoService.CreateTodoAsync(todo);
    }

    [HttpPut("{id}")]
    public Task UpdateTodo(Guid id, CreateTodoRequest todo)
    {
        return todoService.UpdateTodoAsync(id, todo);
    }

    [HttpDelete("{id}")]
    public Task DeleteTodo(Guid id)
    {
        return todoService.DeleteTodoAsync(id);
    }
}
