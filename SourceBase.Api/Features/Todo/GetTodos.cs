using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todo;

public class GetTodos : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/todos", Handler).WithTags("Todos");

    private async Task<Ok<PagingResponse<GetTodoResponse>>> Handler([AsParameters] GetTodosRequest request, IDbContext dbContext, ICurrentUser currentUser, CancellationToken ct)
    {
        var todos = await dbContext.TodoItems
             .Where(x => x.UserId == currentUser.UserId && (request.Status == null || x.Status == request.Status) && (request.Date == null || x.Date == request.Date))
             .PaginateAsync(x => new GetTodoResponse(x), request, ct);
        return TypedResults.Ok(todos);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GetTodosOrder
{
    Date,
    Title,
    Status,
    CreatedOn,
    CreatedBy,
    UpdatedOn,
    UpdatedBy
}

public record GetTodosRequest(TodoItemStatus? Status, DateOnly? Date, int? Page, int? Limit, PagingOrder? Order, GetTodosOrder? OrderBy) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());


