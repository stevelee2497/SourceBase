using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Api.Features.Notifications;

public record MarkNotificationAsReadRequest([property: SwaggerIgnore] Guid Id);

public record MarkNotificationAsReadResponse(bool Success);

public class MarkNotificationAsReadEndpoint : IEndpoint
{
    public const string Route = "notifications/{id}/read";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, ([FromRoute] Guid id, MarkNotificationAsReadHandler handler, CancellationToken ct) => handler.Handle(new MarkNotificationAsReadRequest(id), ct))
        .WithTags("Notifications");
}

public class MarkNotificationAsReadHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<MarkNotificationAsReadRequest, MarkNotificationAsReadResponse>
{
    public async Task<MarkNotificationAsReadResponse> Handle(MarkNotificationAsReadRequest request, CancellationToken ct)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Notification not found.");

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(ct);
        return new MarkNotificationAsReadResponse(true);
    }
}
