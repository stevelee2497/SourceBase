using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todos;

public record CreateTodoRequest(DateOnly? Date, string Title, TodoItemStatus Status, Guid? TodoListId);

public record CreateTodoResponse(Guid Id);

public class CreateTodoEndpoint : IEndpoint
{
    public const string Route = "todos";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTodoRequest request, CreateTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class CreateTodoHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateTodoRequest, CreateTodoResponse>
{
    public async Task<CreateTodoResponse> Handle(CreateTodoRequest request, CancellationToken ct)
    {
        if (request.TodoListId.HasValue)
        {
            var listExists = await dbContext.TodoLists.AnyAsync(x => x.Id == request.TodoListId.Value && x.UserId == currentUser.UserId, ct);
            if (!listExists)
                throw new NotFoundException("Todo list not found.");
        }

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
        return new CreateTodoResponse(item.Id);
    }
}

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Date).NotNull();
        RuleFor(x => x.Title).NotEmpty();
    }
}
