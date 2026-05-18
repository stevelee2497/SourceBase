using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todo;

public class GetTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/todos/{id:guid}", Handler).WithTags("Todos");

    private async Task<Ok<GetTodoResponse>> Handler(Guid id, IDbContext dbContext, CancellationToken ct)
    {
        var todo = await dbContext.TodoItems.FindAsync([id], ct) ?? throw new NotFoundException();
        return TypedResults.Ok(new GetTodoResponse(todo));
    }
}

[method: JsonConstructor]
public record GetTodoResponse(Guid Id, DateOnly Date, string Title, TodoItemStatus Status, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public GetTodoResponse(TodoItemEntity todo) : this(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}
