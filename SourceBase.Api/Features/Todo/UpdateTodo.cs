using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todo;

public class UpdateTodoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut("/todos/{id:guid}", (Guid id, [FromBody] UpdateTodoRequest body, UpdateTodoHandler handler, CancellationToken ct) => handler.Handle(new UpdateTodoCommand(id, body.Date, body.Title, body.Status), ct))
        .WithTags("Todos");
}

public class UpdateTodoHandler(IDbContext dbContext) : IRequestHandler<UpdateTodoCommand, NoContent>
{
    public async Task<NoContent> Handle(UpdateTodoCommand request, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], ct) ?? throw new NotFoundException();
        item.Title = request.Title;
        item.Status = request.Status;
        item.Date = request.Date;
        await dbContext.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}

public record UpdateTodoCommand(Guid Id, DateOnly Date, string Title, TodoItemStatus Status);

public record UpdateTodoRequest(DateOnly Date, string Title, TodoItemStatus Status);

public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
    }
}
