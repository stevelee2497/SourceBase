using MediatR;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Features.Todo;

public record DeleteTodoCommand(Guid Id) : IRequest;

public class DeleteTodoCommandHandler(IDbContext dbContext) : IRequestHandler<DeleteTodoCommand>
{
    public async Task Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.TodoItems.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException();
        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public static class DeleteTodoCommandEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTodoCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/todos/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteTodoCommand(id), cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Todos");

        return endpoints;
    }
}
