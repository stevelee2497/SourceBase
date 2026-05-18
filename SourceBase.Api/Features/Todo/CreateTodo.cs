using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todo;

public class CreateTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/todos", Handler).WithTags("Todos");

    private async Task<NoContent> Handler([FromBody] CreateTodoRequest request, IDbContext dbContext, ICurrentUser currentUser, CancellationToken ct)
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

public record CreateTodoRequest(DateOnly? Date, string Title, TodoItemStatus Status);

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Date).NotNull();
        RuleFor(x => x.Title).NotEmpty();
    }
}
