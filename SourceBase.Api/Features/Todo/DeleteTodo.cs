using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Todo;

public class DeleteTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapDelete("/todos/{id:guid}", Handler).WithTags("Todos");

    private async Task<NoContent> Handler(Guid id, IDbContext dbContext, CancellationToken cancellationToken)
    {
        var item = await dbContext.TodoItems.FindAsync([id], cancellationToken) ?? throw new NotFoundException();
        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
