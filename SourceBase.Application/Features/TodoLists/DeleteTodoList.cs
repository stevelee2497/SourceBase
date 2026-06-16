using FluentValidation;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.TodoLists;

public record DeleteTodoListRequest(Guid Id);

public record DeleteTodoListResponse(bool Success);

public class DeleteTodoListEndpoint : IEndpoint
{
    public const string Route = "todo-lists/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteTodoListRequest request, DeleteTodoListHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TodoLists");
}

public class DeleteTodoListHandler(IDbContext dbContext) : IRequestHandler<DeleteTodoListRequest, DeleteTodoListResponse>
{
    public async Task<DeleteTodoListResponse> Handle(DeleteTodoListRequest request, CancellationToken ct)
    {
        var list = await dbContext.TodoLists.FindAsync([request.Id], ct);
        dbContext.TodoLists.Remove(list!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTodoListResponse(true);
    }
}

public class DeleteTodoListRequestValidator : AbstractValidator<DeleteTodoListRequest>
{
    public DeleteTodoListRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var list = await dbContext.TodoLists.FindAsync([id], ct);
                return list is not null && list.UserId == currentUser.UserId;
            })
            .WithMessage("Todo list not found.");
    }
}
