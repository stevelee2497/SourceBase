using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

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
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        user!.OtpCode = null;
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
    public ResetPasswordRequestValidator(IDbContext dbContext, IDateTime dateTime)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) => await dbContext.Users.AnyAsync(u => u.Email == email, ct))
            .WithMessage("User not found.")
            .When(x => !string.IsNullOrEmpty(x.Email))
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .MustAsync(async (request, ct) =>
                    {
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
                        return user!.OtpCode == request.Code && user.OtpCodeExpiresOn is not null && user.OtpCodeExpiresOn > dateTime.UtcNow;
                    })
                    .WithMessage("Invalid or expired code.");
            });
    }
}
