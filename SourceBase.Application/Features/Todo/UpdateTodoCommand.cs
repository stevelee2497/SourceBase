using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;

namespace SourceBase.Application.Features.Todo;

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
