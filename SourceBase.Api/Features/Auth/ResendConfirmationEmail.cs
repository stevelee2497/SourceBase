using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class ResendConfirmationEmail : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/auth/resendConfirmationEmail", Handler).WithTags("Auth").AllowAnonymous();

    private async Task<NoContent> Handler([FromBody] ResendConfirmationEmailRequest request, UserManager<UserEntity> userManager, IEmailHelper emailHelper, AppSettings appSettings, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (user.EmailConfirmed)
            throw new ApiInternalException("Email already confirmed");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmEmailUrl = $"{appSettings.WebUrl}/confirmEmail?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Please confirm your account by clicking <a href='{confirmEmailUrl}'>here</a>.");
        return TypedResults.NoContent();
    }
}

public record ResendConfirmationEmailRequest(string Email);
