using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Api.Features.Todos;

public record UpdateTodoRequest([property: SwaggerIgnore] Guid Id, DateOnly Date, string Title, TodoItemStatus Status);

public record UpdateTodoResponse(Guid Id);

public class UpdateTodoEndpoint : IEndpoint
{
    public const string Route = "todos/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] UpdateTodoRequest body, UpdateTodoHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .WithTags("Todos");
}

public class UpdateTodoHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateTodoRequest, UpdateTodoResponse>
{
    public async Task<UpdateTodoResponse> Handle(UpdateTodoRequest request, CancellationToken ct)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], ct);
        if (item == null || item.UserId != currentUser.UserId)
            throw new NotFoundException();

        item.Title = request.Title;
        item.Status = request.Status;
        item.Date = request.Date;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateTodoResponse(item.Id);
    }
}

public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
    }
}
