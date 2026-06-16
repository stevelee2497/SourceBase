using FluentValidation;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Todos;

public record DeleteTodoRequest(Guid Id);

public record DeleteTodoResponse(bool Success);

public class DeleteTodoEndpoint : IEndpoint
{
    public const string Route = "todos/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteTodoRequest request, DeleteTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class DeleteTodoHandler(IDbContext dbContext) : IRequestHandler<DeleteTodoRequest, DeleteTodoResponse>
{
    public async Task<DeleteTodoResponse> Handle(DeleteTodoRequest request, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], ct);
        dbContext.TodoItems.Remove(item!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTodoResponse(true);
    }
}

public class DeleteTodoRequestValidator : AbstractValidator<DeleteTodoRequest>
{
    public DeleteTodoRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var item = await dbContext.TodoItems.FindAsync([id], ct);
                return item is not null && item.UserId == currentUser.UserId;
            })
            .WithMessage("Todo not found.");
    }
}