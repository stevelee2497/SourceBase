using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Features.Todo;

namespace SourceBase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/todos")]
public class TodoController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<TodoItemDetailResponse>> GetTodoItems([FromQuery] GetTodosQuery query)
    {
        return sender.Send(query);
    }

    [HttpGet("{id}")]
    public Task<TodoItemDetailResponse> GetTodo([FromRoute] GetTodoQuery query)
    {
        return sender.Send(query);
    }

    [HttpPost]
    public Task CreateTodo([FromBody] CreateTodoCommand command)
    {
        return sender.Send(command);
    }

    [HttpPut("{id}")]
    public Task UpdateTodo(Guid id, [FromBody] UpdateTodoCommand command)
    {
        return sender.Send(command with { Id = id });
    }

    [HttpDelete("{id}")]
    public Task DeleteTodo([FromRoute] DeleteTodoCommand command)
    {
        return sender.Send(command);
    }
}
