using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todo;

public class GetTodos : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/todos", Handler).WithTags("Todos");

    private async Task<Ok<GetTodosResponse>> Handler([AsParameters]GetTodosRequest request, IDbContext dbContext, ICurrentUser currentUser, CancellationToken ct)
    {
       var todos = await dbContext.TodoItems
            .Where(x => x.UserId == currentUser.UserId && (request.Status == null || x.Status == request.Status) && (request.Date == null || x.Date == request.Date))
            .Select(todo => new GetTodoResponse(todo))
            .ToListAsync(ct);
       return TypedResults.Ok(new GetTodosResponse(todos));
    }
}

public record GetTodosRequest(TodoItemStatus? Status, DateOnly? Date);

public record GetTodosResponse(IEnumerable<GetTodoResponse> Items);

