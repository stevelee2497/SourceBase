using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;

namespace SourceBase.Application.Features.Todo;

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
