using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todo;

public class DeleteTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapDelete("/todos/{id:guid}", Handler).WithTags("Todos");

    private async Task<NoContent> Handler(Guid id, IDbContext dbContext, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([id], ct) ?? throw new NotFoundException();
        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
