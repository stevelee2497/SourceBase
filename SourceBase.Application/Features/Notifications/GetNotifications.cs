using System.Text.Json.Serialization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Notifications;

public record NotificationItem(Guid Id, string Title, string Message, bool IsRead, DateTime? CreatedOn);

public record GetNotificationsRequest(bool? UnreadOnly = false, int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Desc, NotificationOrderBy OrderBy = NotificationOrderBy.CreatedOn) : PagingRequest(Page, Limit, Order, OrderBy.ToString());

public class GetNotificationsEndpoint : IEndpoint
{
    public const string Route = "notifications";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetNotificationsRequest request, GetNotificationsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Notifications");
}

public class GetNotificationsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetNotificationsRequest, PagingResponse<NotificationItem>>
{
    public async Task<PagingResponse<NotificationItem>> Handle(GetNotificationsRequest request, CancellationToken ct)
    {
        var query = dbContext.Notifications
            .Where(n => n.UserId == currentUser.UserId);

        if (request.UnreadOnly == true)
            query = query.Where(n => !n.IsRead);

        return await query.PaginateAsync(n => new NotificationItem(n.Id, n.Title, n.Message, n.IsRead, n.CreatedOn), request, ct);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationOrderBy
{
    CreatedOn,
    Title,
    IsRead
}
