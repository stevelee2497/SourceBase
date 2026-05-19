using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class ForgotPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/auth/forgotPassword", Handler).WithTags("Auth").AllowAnonymous();

    private async Task<NoContent> Handler([FromBody] ForgotPasswordRequest request, UserManager<UserEntity> userManager, IEmailHelper emailHelper, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var otp = OtpHelper.Generate();
        user.OtpCode = otp;
        await userManager.UpdateAsync(user);
        await emailHelper.SendEmailAsync(request.Email, "Reset Password", $"Your password reset code is: <b>{otp}</b>");
        return TypedResults.NoContent();
    }
}

public record ForgotPasswordRequest(string Email);

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
