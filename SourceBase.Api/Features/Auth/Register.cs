using FluentValidation;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class Register : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/auth/register", Handler).WithTags("Auth").AllowAnonymous();

    private async Task<NoContent> Handler([FromBody] RegisterRequest request, UserManager<UserEntity> userManager, IEmailHelper emailHelper, AppSettings appSettings, CancellationToken ct)
    {
        var user = new UserEntity { Email = request.Email, UserName = request.Email };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BadRequestException(createResult.Errors.First().Description);

        var persistedUser = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (persistedUser.EmailConfirmed)
            throw new BadRequestException("Email already confirmed");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(persistedUser);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmEmailUrl = $"{appSettings.WebUrl}/confirmEmail?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Please confirm your account by clicking <a href='{confirmEmailUrl}'>here</a>.");
        return TypedResults.NoContent();
    }
}

public record RegisterRequest(string Email, string Password);

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
