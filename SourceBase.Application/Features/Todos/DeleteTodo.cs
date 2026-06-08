using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Todos;

public record DeleteTodoRequest(Guid Id);

public record DeleteTodoResponse(bool Success);

public class DeleteTodoEndpoint : IEndpoint
{
    public const string Route = "todos/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteTodoHandler handler, CancellationToken ct) => handler.Handle(new DeleteTodoRequest(id), ct))
        .WithTags("Todos");
}

public class DeleteTodoHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteTodoRequest, DeleteTodoResponse>
{
    public async Task<DeleteTodoResponse> Handle(DeleteTodoRequest request, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], ct);
        if (item == null || item.UserId != currentUser.UserId)
            throw new NotFoundException();

        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTodoResponse(true);
    }
}