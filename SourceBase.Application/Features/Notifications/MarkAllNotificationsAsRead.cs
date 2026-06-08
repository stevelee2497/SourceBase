using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Notifications;

public record MarkAllNotificationsAsReadRequest;

public record MarkAllNotificationsAsReadResponse(bool Success);

public class MarkAllNotificationsAsReadEndpoint : IEndpoint
{
    public const string Route = "notifications/read-all";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (MarkAllNotificationsAsReadHandler handler, CancellationToken ct) => handler.Handle(new MarkAllNotificationsAsReadRequest(), ct))
        .WithTags("Notifications");
}

public class MarkAllNotificationsAsReadHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<MarkAllNotificationsAsReadRequest, MarkAllNotificationsAsReadResponse>
{
    public async Task<MarkAllNotificationsAsReadResponse> Handle(MarkAllNotificationsAsReadRequest request, CancellationToken ct)
    {
        await dbContext.Notifications
            .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

        return new MarkAllNotificationsAsReadResponse(true);
    }
}
