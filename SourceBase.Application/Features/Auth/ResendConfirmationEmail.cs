using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record ResendConfirmationEmailRequest(string Email);

public record ResendConfirmationEmailResponse(bool Success);

public class ResendConfirmationEmailEndpoint : IEndpoint
{
    public const string Route = "auth/resendConfirmationEmail";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] ResendConfirmationEmailRequest request, ResendConfirmationEmailHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ResendConfirmationEmailHandler(IDbContext dbContext, IEmailHelper emailHelper, IOtpHelper otpHelper) : IRequestHandler<ResendConfirmationEmailRequest, ResendConfirmationEmailResponse>
{
    public async Task<ResendConfirmationEmailResponse> Handle(ResendConfirmationEmailRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        var (otp, expiresOn) = otpHelper.Generate();
        user!.OtpCode = otp;
        user.OtpCodeExpiresOn = expiresOn;
        await dbContext.SaveChangesAsync(ct);

        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Your confirmation code is: <b>{otp}</b>");
        return new ResendConfirmationEmailResponse(true);
    }
}

public class ResendConfirmationEmailRequestValidator : AbstractValidator<ResendConfirmationEmailRequest>
{
    public ResendConfirmationEmailRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) => await dbContext.Users.AnyAsync(u => u.Email == email, ct))
            .WithMessage("User not found.")
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
                return user is null || !user.EmailConfirmed;
            })
            .WithMessage("Email already confirmed.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
