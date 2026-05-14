using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler(UserManager<ApplicationUser> userManager, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetPasswordUrl = $"{appSettings.WebUrl}/resetPassword?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Reset Password", $"Click <a href='{resetPasswordUrl}'>here</a> to reset your password.");
    }
}

public static class ForgotPasswordCommandEndpoint
{
    public static IEndpointRouteBuilder MapForgotPasswordCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/forgotPassword", async (ForgotPasswordCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Auth")
            .AllowAnonymous();

        return endpoints;
    }
}
