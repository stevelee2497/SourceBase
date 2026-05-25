using System.Text.Json.Serialization;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Todos;

public record GetTodosRequest(TodoItemStatus? Status, DateOnly? Date, int? Page, int? Limit, PagingOrder? Order, GetTodosOrder? OrderBy) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public class GetTodosEndpoint : IEndpoint
{
    public const string Route = "todos";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTodosRequest request, GetTodosHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class GetTodosHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTodosRequest, PagingResponse<GetTodoResponse>>
{
    public async Task<PagingResponse<GetTodoResponse>> Handle(GetTodosRequest request, CancellationToken ct)
    {
        var todos = await dbContext.TodoItems
            .Where(x => x.UserId == currentUser.UserId && (request.Status == null || x.Status == request.Status) && (request.Date == null || x.Date == request.Date))
            .PaginateAsync(x => new GetTodoResponse(x), request, ct);
        return todos;
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
