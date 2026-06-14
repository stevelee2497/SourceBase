using System.Text.Json.Serialization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.TodoLists;

public record GetTodoListsRequest(int? Page = 1, int? Limit = 20, PagingOrder? Order = PagingOrder.Desc, TodoListsOrder? OrderBy = TodoListsOrder.CreatedOn) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public record TodoListResponse(Guid Id, string Name, int ItemCount, DateTime? CreatedOn, string? CreatedBy, bool IsDefault);

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
        var user = await dbContext.Users.FindAsync([currentUser.UserId], ct);
        var defaultId = user?.DefaultTodoListId;
        var lists = await dbContext.TodoLists
            .Where(x => x.UserId == currentUser.UserId)
            .PaginateAsync(x => new TodoListResponse(x.Id, x.Name, x.Items.Count, x.CreatedOn, x.CreatedBy, x.Id == defaultId), request, ct);
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
