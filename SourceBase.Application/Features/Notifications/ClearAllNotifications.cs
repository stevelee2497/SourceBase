using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Notifications;

public record ClearAllNotificationsRequest;

public record ClearAllNotificationsResponse(bool Success);

public class ClearAllNotificationsEndpoint : IEndpoint
{
    public const string Route = "notifications";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (ClearAllNotificationsHandler handler, CancellationToken ct) => handler.Handle(new ClearAllNotificationsRequest(), ct))
        .WithTags("Notifications");
}

public class ClearAllNotificationsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<ClearAllNotificationsRequest, ClearAllNotificationsResponse>
{
    public async Task<ClearAllNotificationsResponse> Handle(ClearAllNotificationsRequest request, CancellationToken ct)
    {
        await dbContext.Notifications
            .Where(n => n.UserId == currentUser.UserId)
            .ExecuteDeleteAsync(ct);

        return new ClearAllNotificationsResponse(true);
    }
}
