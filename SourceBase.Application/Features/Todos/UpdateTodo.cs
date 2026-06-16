using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Todos;

public record UpdateTodoRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, DateOnly? Date, string? Title, TodoItemStatus? Status);

public record UpdateTodoResponse(Guid Id);

public class UpdateTodoEndpoint : IEndpoint
{
    public const string Route = "todos/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateTodoRequest body, UpdateTodoHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Todos");
}

public class UpdateTodoHandler(IDbContext dbContext, INotificationService notifications) : IRequestHandler<UpdateTodoRequest, UpdateTodoResponse>
{
    public async Task<UpdateTodoResponse> Handle(UpdateTodoRequest request, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], ct)!;

        item!.Title = request.Title ?? item.Title;
        item.Status = request.Status ?? item.Status;
        item.Date = request.Date ?? item.Date;
        await dbContext.SaveChangesAsync(ct);
        await notifications.CreateAsync(new NotificationEntity
        {
            UserId = item.UserId,
            Event = NotificationEvent.TodoUpdatedEvent,
            Title = "Todo item updated",
            Message = $"Your todo item {item.Title} has been updated.",
            Data = item.Serialize(),
            IsRead = true,
        }, ct);
        return new UpdateTodoResponse(item.Id);
    }
}

public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var item = await dbContext.TodoItems.FindAsync([id], ct);
                return item is not null && item.UserId == currentUser.UserId;
            })
            .WithMessage("Todo item not found.");

        RuleFor(x => x.Title).NotEmpty().When(x => x.Title is not null);
    }
}
