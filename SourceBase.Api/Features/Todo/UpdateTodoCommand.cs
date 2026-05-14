using MediatR;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Features.Todo;

public record UpdateTodoCommand(Guid Id, DateOnly Date, string Title, ItemStatus Status) : IRequest;

public class UpdateTodoCommandHandler(IDbContext dbContext) : IRequestHandler<UpdateTodoCommand>
{
    public async Task Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException();
        item.Title = request.Title;
        item.Status = request.Status;
        item.Date = request.Date;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public static class UpdateTodoCommandEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTodoCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/todos/{id:guid}", async (Guid id, UpdateTodoCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command with { Id = id }, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Todos");

        return endpoints;
    }
}
