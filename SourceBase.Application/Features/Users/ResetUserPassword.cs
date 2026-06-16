using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Users;

public record ResetUserPasswordRequest([property: SwaggerIgnore] Guid Id, string NewPassword);

public record ResetUserPasswordResponse(bool Success);

public class ResetUserPasswordEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}/reset-password";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] ResetUserPasswordRequest body, ResetUserPasswordHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class ResetUserPasswordHandler(IDbContext dbContext, ISecurityProvider securityProvider, IEmailHelper emailHelper) : IRequestHandler<ResetUserPasswordRequest, ResetUserPasswordResponse>
{
    public async Task<ResetUserPasswordResponse> Handle(ResetUserPasswordRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException();

        user.PasswordHash = securityProvider.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();

        await dbContext.SaveChangesAsync(ct);

        await emailHelper.SendEmailAsync(user.Email!, "Your password has been reset",
            $"Your account password has been reset by an administrator. Your new password is: <b>{request.NewPassword}</b><br/>Please change it after logging in.");

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
