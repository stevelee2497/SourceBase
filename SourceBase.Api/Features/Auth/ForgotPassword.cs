using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record ForgotPasswordRequest(string Email);

public record ForgotPasswordResponse(bool Success);

public class ForgotPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/forgotPassword", ([FromBody] ForgotPasswordRequest request, ForgotPasswordHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ForgotPasswordHandler(UserManager<UserEntity> userManager, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<ForgotPasswordRequest, ForgotPasswordResponse>
{
    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var (otp, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
        user.OtpCode = otp;
        user.OtpCodeExpiresOn = expiresOn;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BadRequestException(updateResult.Errors.First().Description);

        await emailHelper.SendEmailAsync(request.Email, "Reset Password", $"Your password reset code is: <b>{otp}</b>");
        return new ForgotPasswordResponse(true);
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
