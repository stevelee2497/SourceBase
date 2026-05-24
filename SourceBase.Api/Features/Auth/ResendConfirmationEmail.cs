using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record ResendConfirmationEmailRequest(string Email);

public record ResendConfirmationEmailResponse(bool Success);

public class ResendConfirmationEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/resendConfirmationEmail", ([FromBody] ResendConfirmationEmailRequest request, ResendConfirmationEmailHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ResendConfirmationEmailHandler(UserManager<UserEntity> userManager, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<ResendConfirmationEmailRequest, ResendConfirmationEmailResponse>
{
    public async Task<ResendConfirmationEmailResponse> Handle(ResendConfirmationEmailRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (user.EmailConfirmed)
            throw new BadRequestException("Email already confirmed");

        var (otp, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
        user.OtpCode = otp;
        user.OtpCodeExpiresOn = expiresOn;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BadRequestException(updateResult.Errors.First().Description);

        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Your confirmation code is: <b>{otp}</b>");
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
