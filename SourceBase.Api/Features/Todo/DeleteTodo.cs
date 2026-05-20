using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todo;

public class DeleteTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapDelete("/todos/{id:guid}",
        (Guid id, ISender sender, CancellationToken ct) => sender.Send(new DeleteTodoCommand(id), ct)).WithTags("Todos");
}

public class DeleteTodoHandler(IDbContext dbContext) : IRequestHandler<DeleteTodoCommand, NoContent>
{
    public async Task<NoContent> Handle(DeleteTodoCommand request, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], ct) ?? throw new NotFoundException();
        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}

public record DeleteTodoCommand(Guid Id) : IRequest<NoContent>;
