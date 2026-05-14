using System.ComponentModel.DataAnnotations;
using MediatR;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Features.Todo;

public record CreateTodoCommand([Required] DateOnly? Date, [Required] string Title, ItemStatus Status) : IRequest;

public class CreateTodoCommandHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateTodoCommand>
{
    public async Task Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        dbContext.TodoItems.Add(new TodoItemEntity
        {
            Title = request.Title,
            Date = request.Date!.Value,
            Status = request.Status,
            UserId = currentUser.UserId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public static class CreateTodoCommandEndpoint
{
    public static IEndpointRouteBuilder MapCreateTodoCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/todos", async (CreateTodoCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Todos");

        return endpoints;
    }
}
