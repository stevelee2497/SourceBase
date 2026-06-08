using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;

namespace SourceBase.Application.Features.TodoLists;

public record DeleteTodoListRequest(Guid Id);

public record DeleteTodoListResponse(bool Success);

public class DeleteTodoListEndpoint : IEndpoint
{
    public const string Route = "todo-lists/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteTodoListHandler handler, CancellationToken ct) => handler.Handle(new DeleteTodoListRequest(id), ct))
        .WithTags("TodoLists");
}

public class DeleteTodoListHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteTodoListRequest, DeleteTodoListResponse>
{
    public async Task<DeleteTodoListResponse> Handle(DeleteTodoListRequest request, CancellationToken ct)
    {
        var list = await dbContext.TodoLists.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        dbContext.TodoLists.Remove(list);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTodoListResponse(true);
    }
}
