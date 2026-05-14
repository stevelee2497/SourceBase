using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Helpers;

namespace SourceBase.Api.Features.Auth;

public record RegisterCommand(string Email, string Password) : IRequest;

public class RegisterCommandHandler(UserManager<ApplicationUser> userManager, SendGridEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<RegisterCommand>
{
    public async Task Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { Email = request.Email, UserName = request.Email };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new ApiInternalException(createResult.Errors.First().Description);
        }

        var persistedUser = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (persistedUser.EmailConfirmed)
        {
            throw new ApiInternalException("Email already confirmed");
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(persistedUser);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmEmailUrl = $"{appSettings.WebUrl}/confirmEmail?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Please confirm your account by clicking <a href='{confirmEmailUrl}'>here</a>.");
    }
}

public static class RegisterCommandEndpoint
{
    public static IEndpointRouteBuilder MapRegisterCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/register", async (RegisterCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Auth")
            .AllowAnonymous();

        return endpoints;
    }
}
