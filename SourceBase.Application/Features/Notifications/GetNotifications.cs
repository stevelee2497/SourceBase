using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Notifications;

public record NotificationItem(Guid Id, string Title, string Message, bool IsRead, DateTime? CreatedOn);

public record GetNotificationsRequest(int? Page = 1, int? Limit = 10, bool? UnreadOnly = false);

public record GetNotificationsResponse(List<NotificationItem> Items, int Page, int Limit, int Total);

public class GetNotificationsEndpoint : IEndpoint
{
    public const string Route = "notifications";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetNotificationsRequest request, GetNotificationsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Notifications");
}

public class GetNotificationsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetNotificationsRequest, GetNotificationsResponse>
{
    public async Task<GetNotificationsResponse> Handle(GetNotificationsRequest request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page ?? 1);
        var limit = Math.Max(1, Math.Min(100, request.Limit ?? 10));

        var query = dbContext.Notifications
            .Where(n => n.UserId == currentUser.UserId);

        if (request.UnreadOnly == true)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedOn)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(n => new NotificationItem(n.Id, n.Title, n.Message, n.IsRead, n.CreatedOn))
            .ToListAsync(ct);

        return new GetNotificationsResponse(items, page, limit, total);
    }
}
