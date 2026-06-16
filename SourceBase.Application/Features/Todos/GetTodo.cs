using FluentValidation;
using System.Text.Json.Serialization;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Todos;

public record GetTodoRequest(Guid Id);

[method: JsonConstructor]
public record GetTodoResponse(Guid Id, Guid UserId, DateOnly Date, string Title, TodoItemStatus Status, Guid? TodoListId, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public GetTodoResponse(TodoItemEntity todo) : this(todo.Id, todo.UserId, todo.Date, todo.Title, todo.Status, todo.TodoListId, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}

public class GetTodoEndpoint : IEndpoint
{
    public const string Route = "todos/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTodoRequest request, GetTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class GetTodoHandler(IDbContext dbContext) : IRequestHandler<GetTodoRequest, GetTodoResponse>
{
    public async Task<GetTodoResponse> Handle(GetTodoRequest request, CancellationToken ct)
    {
        var todo = await dbContext.TodoItems.FindAsync([request.Id], ct);
        return new GetTodoResponse(todo!);
    }
}

public class GetTodoRequestValidator : AbstractValidator<GetTodoRequest>
{
    public GetTodoRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
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
