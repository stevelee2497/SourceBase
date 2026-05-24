using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todos;

public record DeleteTodoRequest(Guid Id);

public record DeleteTodoResponse(bool Success);

public class DeleteTodoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete("/todos/{id:guid}", (Guid id, DeleteTodoHandler handler, CancellationToken ct) => handler.Handle(new DeleteTodoRequest(id), ct))
        .WithTags("Todos");
}

public class DeleteTodoHandler(IDbContext dbContext) : IRequestHandler<DeleteTodoRequest, DeleteTodoResponse>
{
    public async Task<DeleteTodoResponse> Handle(DeleteTodoRequest request, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], ct) ?? throw new NotFoundException();
        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTodoResponse(true);
    }
}
