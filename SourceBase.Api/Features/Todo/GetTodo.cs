using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Common;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Todo;

public class GetTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/todos/{id:guid}", Handler).WithTags("Todos");

    private async Task<Ok<TodoItemDetailResponse>> Handler(Guid id, IDbContext dbContext, CancellationToken cancellationToken)
    {
        var todo = await dbContext.TodoItems.FindAsync([id], cancellationToken) ?? throw new NotFoundException();
        return TypedResults.Ok(new TodoItemDetailResponse(todo));
    }
}

public record TodoItemDetailResponse(Guid Id, DateOnly Date, string Title, TodoItemStatus Status, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public TodoItemDetailResponse(TodoItemEntity todo)
        : this(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}
