using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
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
        .RequireRateLimiting(Constants.StrictRateLimitPolicy)
        .WithTags("Auth");
}

public class ResendConfirmationEmailHandler(IDbContext dbContext, IMessageQueuePublisher messageQueuePublisher, IOtpHelper otpHelper) : IRequestHandler<ResendConfirmationEmailRequest, ResendConfirmationEmailResponse>
{
    public async Task<ResendConfirmationEmailResponse> Handle(ResendConfirmationEmailRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct) ?? throw new NotFoundException("User not found");
        if (user.EmailConfirmed)
            throw new BadRequestException("Email already confirmed");

        var (otp, expiresOn) = otpHelper.Generate();
        user.OtpCode = otp;
        user.OtpCodeExpiresOn = expiresOn;
        dbContext.Emails.Add(new EmailEntity(request.Email, "Confirm your email", $"Your confirmation code is: <b>{otp}</b>"));
        await dbContext.SaveChangesAsync(ct);

        await messageQueuePublisher.PublishAsync("email", new EmailMessage(request.Email, "Confirm your email", $"Your confirmation code is: <b>{otp}</b>"), ct);
        return new ResendConfirmationEmailResponse(true);
    }
}

public class ResendConfirmationEmailRequestValidator : AbstractValidator<ResendConfirmationEmailRequest>
{
    public ResendConfirmationEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
