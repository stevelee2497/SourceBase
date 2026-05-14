using MediatR;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Infrastructure.DbContexts;
using SourceBase.Api.Infrastructure.Identity;

namespace SourceBase.Api.Features.Todo;

public record GetTodosQuery() : IRequest<IEnumerable<TodoItemDetailResponse>>;

public class GetTodosQueryHandler(ApplicationDbContext dbContext, CurrentUser currentUser) : IRequestHandler<GetTodosQuery, IEnumerable<TodoItemDetailResponse>>
{
    public async Task<IEnumerable<TodoItemDetailResponse>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TodoItems
            .Where(x => x.UserId == currentUser.UserId)
            .Select(todo => new TodoItemDetailResponse(todo))
            .ToListAsync(cancellationToken);
    }
}

public static class GetTodosQueryEndpoint
{
    public static IEndpointRouteBuilder MapGetTodosQueryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/todos", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetTodosQuery(), cancellationToken)))
            .WithTags("Todos");

        return endpoints;
    }
}
