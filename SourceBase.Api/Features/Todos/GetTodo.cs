using System.Text.Json.Serialization;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todos;

public record GetTodoRequest(Guid Id);

[method: JsonConstructor]
public record GetTodoResponse(Guid Id, DateOnly Date, string Title, TodoItemStatus Status, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public GetTodoResponse(TodoItemEntity todo) : this(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}

public class GetTodoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet("/todos/{id:guid}", (Guid id, GetTodoHandler handler, CancellationToken ct) => handler.Handle(new GetTodoRequest(id), ct))
        .WithTags("Todos");
}

public class GetTodoHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTodoRequest, GetTodoResponse>
{
    public async Task<GetTodoResponse> Handle(GetTodoRequest request, CancellationToken ct)
    {
        var todo = await dbContext.TodoItems.FindAsync([request.Id], ct);

        if (todo is null || todo.UserId != currentUser.UserId)
        {
            throw new NotFoundException(); // Don't reveal existence of the todo if the user doesn't own it
        }

        return new GetTodoResponse(todo);
    }
}
