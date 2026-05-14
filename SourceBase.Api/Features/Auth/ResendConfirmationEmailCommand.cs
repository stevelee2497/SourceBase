using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record ResendConfirmationEmailCommand(string Email) : IRequest;

public class ResendConfirmationEmailCommandHandler(UserManager<ApplicationUser> userManager, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<ResendConfirmationEmailCommand>
{
    public async Task Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (user.EmailConfirmed)
        {
            throw new ApiInternalException("Email already confirmed");
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmEmailUrl = $"{appSettings.WebUrl}/confirmEmail?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Please confirm your account by clicking <a href='{confirmEmailUrl}'>here</a>.");
    }
}

public static class ResendConfirmationEmailCommandEndpoint
{
    public static IEndpointRouteBuilder MapResendConfirmationEmailCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/resendConfirmationEmail", async (ResendConfirmationEmailCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Auth")
            .AllowAnonymous();

        return endpoints;
    }
}
