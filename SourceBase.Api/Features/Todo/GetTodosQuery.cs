using MediatR;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Todo;

public record GetTodosQuery() : IRequest<IEnumerable<TodoItemDetailResponse>>;

public class GetTodosQueryHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTodosQuery, IEnumerable<TodoItemDetailResponse>>
{
    public async Task<IEnumerable<TodoItemDetailResponse>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TodoItems
            .Where(x => x.UserId == currentUser.UserId)
            .Select(todo => new TodoItemDetailResponse(todo))
            .ToListAsync(cancellationToken);
    }
}

public class GetTodosQueryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/todos", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetTodosQuery(), cancellationToken)))
            .WithTags("Todos");
}
