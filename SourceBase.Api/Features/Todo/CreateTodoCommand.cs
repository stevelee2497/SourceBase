using MediatR;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Infrastructure.DbContexts;
using SourceBase.Api.Infrastructure.Identity;

namespace SourceBase.Api.Features.Todo;

public record CreateTodoCommand(DateOnly Date, string Title, ItemStatus Status) : IRequest;

public class CreateTodoCommandHandler(ApplicationDbContext dbContext, CurrentUser currentUser) : IRequestHandler<CreateTodoCommand>
{
    public async Task Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        dbContext.TodoItems.Add(new TodoItemEntity
        {
            Title = request.Title,
            Date = request.Date,
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
