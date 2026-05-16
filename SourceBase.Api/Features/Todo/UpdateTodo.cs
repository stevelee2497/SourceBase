using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Common;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Todo;

public class UpdateTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPut("/todos/{id:guid}", Handler).WithTags("Todos");

    private async Task<NoContent> Handler(Guid id, [FromBody] UpdateTodoRequest request, IDbContext dbContext, CancellationToken cancellationToken)
    {
        var item = await dbContext.TodoItems.FindAsync([id], cancellationToken) ?? throw new NotFoundException();
        item.Title = request.Title;
        item.Status = request.Status;
        item.Date = request.Date;
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}

public record UpdateTodoRequest(DateOnly Date, string Title, TodoItemStatus Status);
