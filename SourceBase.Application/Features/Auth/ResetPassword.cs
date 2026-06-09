using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Auth;

public record ResetPasswordRequest(string Email, string Code, string NewPassword);

public record ResetPasswordResponse(bool Success);

public class ResetPasswordEndpoint : IEndpoint
{
    public const string Route = "auth/resetPassword";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] ResetPasswordRequest request, ResetPasswordHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ResetPasswordHandler(IDbContext dbContext, ISecurityProvider securityProvider) : IRequestHandler<ResetPasswordRequest, ResetPasswordResponse>
{
    public async Task<ResetPasswordResponse> Handle(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct) ?? throw new NotFoundException("User not found");
        if (user.OtpCode != request.Code || user.OtpCodeExpiresOn is null || user.OtpCodeExpiresOn <= DateTime.UtcNow)
            throw new BadRequestException("Invalid or expired code");

        user.OtpCode = null;
        user.OtpCodeExpiresOn = null;
        user.EmailConfirmed = true; // Ensure email is confirmed after password reset
        user.PasswordHash = securityProvider.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens
        await dbContext.SaveChangesAsync(ct);

        return new ResetPasswordResponse(true);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
