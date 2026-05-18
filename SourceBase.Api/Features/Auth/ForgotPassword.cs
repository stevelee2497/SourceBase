using FluentValidation;
using System.Text;
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

    private async Task<NoContent> Handler([FromBody] ForgotPasswordRequest request, UserManager<UserEntity> userManager, IEmailHelper emailHelper, AppSettings appSettings, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetPasswordUrl = $"{appSettings.WebUrl}/resetPassword?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Reset Password", $"Click <a href='{resetPasswordUrl}'>here</a> to reset your password.");
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
