using Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/todos")]
public class TodoController(ITodoService todoService) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<TodoItemDetailResponse>> GetTodoItems()
    {
        return await todoService.GetTodosAsync();
    }

    [HttpGet("{id}")]
    public async Task<TodoItemDetailResponse> GetTodo(Guid id)
    {
        return await todoService.GetTodoAsync(id);
    }

    [HttpPost]
    public async Task CreateTodo(CreateTodoRequest todo)
    {
        await todoService.CreateTodoAsync(todo);
    }

    [HttpPut("{id}")]
    public async Task UpdateTodo(Guid id, CreateTodoRequest todo)
    {
        await todoService.UpdateTodoAsync(id, todo);
    }

    [HttpDelete("{id}")]
    public async Task DeleteTodo(Guid id)
    {
        await todoService.DeleteTodoAsync(id);
    }
}
