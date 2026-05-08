using MediatR;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Todo;

public record GetTodosQuery() : IRequest<IEnumerable<TodoItemDetailResponse>>;

public class GetTodosQueryHandler(IDbContext dbContext, IUserContext userContext) : IRequestHandler<GetTodosQuery, IEnumerable<TodoItemDetailResponse>>
{
    public async Task<IEnumerable<TodoItemDetailResponse>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TodoItems
            .Where(x => x.UserId == userContext.UserId)
            .Select(todo => new TodoItemDetailResponse(todo))
            .ToListAsync(cancellationToken);
    }
}
