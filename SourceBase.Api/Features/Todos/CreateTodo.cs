using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todos;

public record CreateTodoRequest(DateOnly? Date, string Title, TodoItemStatus Status);

public class CreateTodoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/todos", ([FromBody] CreateTodoRequest request, CreateTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class CreateTodoHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateTodoRequest, NoContent>
{
    public async Task<NoContent> Handle(CreateTodoRequest request, CancellationToken ct)
    {
        dbContext.TodoItems.Add(new TodoItemEntity
        {
            Title = request.Title,
            Date = request.Date!.Value,
            Status = request.Status,
            UserId = currentUser.UserId,
        });
        await dbContext.SaveChangesAsync(ct);
        return TypedResults.NoContent();
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
