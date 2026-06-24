using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Users;

public record ResetUserPasswordRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string NewPassword);

public record ResetUserPasswordResponse(bool Success);

public class ResetUserPasswordEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}/reset-password";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] ResetUserPasswordRequest body, ResetUserPasswordHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class ResetUserPasswordHandler(IDbContext dbContext, ISecurityProvider securityProvider, IMessageQueuePublisher messageQueuePublisher) : IRequestHandler<ResetUserPasswordRequest, ResetUserPasswordResponse>
{
    public async Task<ResetUserPasswordResponse> Handle(ResetUserPasswordRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException();

        user.PasswordHash = securityProvider.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();

        var subject = "Your password has been reset";
        var body = $"Your account password has been reset by an administrator. Your new password is: <b>{request.NewPassword}</b><br/>Please change it after logging in.";
        dbContext.Emails.Add(new EmailEntity(user.Email!, subject, body));
        await dbContext.SaveChangesAsync(ct);

        await messageQueuePublisher.PublishAsync("email", new EmailMessage(user.Email!, subject, body), ct);

        return new ResetUserPasswordResponse(true);
    }
}

public class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
