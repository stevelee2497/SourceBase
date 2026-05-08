using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Domain.Entities;

namespace SourceBase.Application.Features.Todo;

public record CreateTodoCommand(DateOnly Date, string Title, ItemStatus Status) : IRequest;

public class CreateTodoCommandHandler(IDbContext dbContext, IUserContext userContext) : IRequestHandler<CreateTodoCommand>
{
    public async Task Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        dbContext.TodoItems.Add(new TodoItemEntity { Title = request.Title, Date = request.Date, UserId = userContext.UserId });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
