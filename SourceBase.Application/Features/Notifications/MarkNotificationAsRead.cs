using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using FluentValidation;

namespace SourceBase.Application.Features.Notifications;

public record MarkNotificationAsReadRequest([property: SwaggerIgnore][property: FromRoute] Guid Id);

public record MarkNotificationAsReadResponse(bool Success);

public class MarkNotificationAsReadEndpoint : IEndpoint
{
    public const string Route = "notifications/{id}/read";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, ([FromBody] MarkNotificationAsReadRequest request, MarkNotificationAsReadHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Notifications");
}

public class MarkNotificationAsReadHandler(IDbContext dbContext) : IRequestHandler<MarkNotificationAsReadRequest, MarkNotificationAsReadResponse>
{
    public async Task<MarkNotificationAsReadResponse> Handle(MarkNotificationAsReadRequest request, CancellationToken ct)
    {
        var notification = await dbContext.Notifications.FindAsync([request.Id], ct);
        notification!.IsRead = true;
        await dbContext.SaveChangesAsync(ct);
        return new MarkNotificationAsReadResponse(true);
    }
}

public class MarkNotificationAsReadRequestValidator : AbstractValidator<MarkNotificationAsReadRequest>
{
    public MarkNotificationAsReadRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id).NotEmpty().MustAsync(async (id, ct) =>
        {
            var noti = await dbContext.Notifications.FindAsync([id], ct);
            return noti is not null && noti.UserId == currentUser.UserId;
        }).WithMessage("Notification not found.");
    }
}