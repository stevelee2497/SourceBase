using System.Text.Json.Serialization;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.TodoLists;

public record GetTodoListsRequest(int? Page = 1, int? Limit = 20, PagingOrder? Order = PagingOrder.Desc, TodoListsOrder? OrderBy = TodoListsOrder.CreatedOn) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public record TodoListResponse(Guid Id, string Name, int ItemCount, DateTime? CreatedOn, string? CreatedBy);

public class GetTodoListsEndpoint : IEndpoint
{
    public const string Route = "todo-lists";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTodoListsRequest request, GetTodoListsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TodoLists");
}

public class GetTodoListsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTodoListsRequest, PagingResponse<TodoListResponse>>
{
    public async Task<PagingResponse<TodoListResponse>> Handle(GetTodoListsRequest request, CancellationToken ct)
    {
        var lists = await dbContext.TodoLists
            .Where(x => x.UserId == currentUser.UserId)
            .PaginateAsync(x => new TodoListResponse(x.Id, x.Name, x.Items.Count, x.CreatedOn, x.CreatedBy), request, ct);
        return lists;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoListsOrder
{
    Name,
    CreatedOn,
    UpdatedOn
}
