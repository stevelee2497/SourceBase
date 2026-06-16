using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Todos;

public record CreateTodoRequest(DateOnly? Date, string Title, TodoItemStatus Status, Guid? TodoListId);

public record CreateTodoResponse(Guid Id);

public class CreateTodoEndpoint : IEndpoint
{
    public const string Route = "todos";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTodoRequest request, CreateTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class CreateTodoHandler(IDbContext dbContext, ICurrentUser currentUser, INotificationService notifications) : IRequestHandler<CreateTodoRequest, CreateTodoResponse>
{
    public async Task<CreateTodoResponse> Handle(CreateTodoRequest request, CancellationToken ct)
    {
        var item = new TodoItemEntity
        {
            Title = request.Title,
            Date = request.Date!.Value,
            Status = request.Status,
            UserId = currentUser.UserId,
            TodoListId = request.TodoListId,
        };
        dbContext.TodoItems.Add(item);
        await dbContext.SaveChangesAsync(ct);
        await notifications.CreateAsync(new NotificationEntity
        {
            UserId = item.UserId,
            Event = NotificationEvent.TodoCreatedEvent,
            Title = "Todo item created",
            Message = $"A new todo item {item.Title} has been created.",
            Data = item.Serialize(),
            IsRead = true,
        }, ct);
        return new CreateTodoResponse(item.Id);
    }
}

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Date).NotNull();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.TodoListId)
            .MustAsync(async (id, ct) =>
            {
                var list = await dbContext.TodoLists.FindAsync([id!.Value], ct);
                return list is not null && list.UserId == currentUser.UserId;
            })
            .WithMessage("Todo list not found.")
            .When(x => x.TodoListId.HasValue);
    }
}
