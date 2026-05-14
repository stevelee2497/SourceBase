using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;

namespace SourceBase.Api.Features.Auth;

public record ConfirmEmailCommand(string Email, string Code) : IRequest;

public class ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<ConfirmEmailCommand>
{
    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new UnAuthorizedException();

        var decodedCode = Encoding.UTF8.GetString(Base64UrlHelper.Base64UrlDecode(request.Code));
        var result = await userManager.ConfirmEmailAsync(user, decodedCode);
        if (!result.Succeeded)
        {
            throw new UnAuthorizedException();
        }

        await userManager.AddToRoleAsync(user, Roles.User);
    }
}

public static class ConfirmEmailCommandEndpoint
{
    public static IEndpointRouteBuilder MapConfirmEmailCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/confirmEmail", async (ConfirmEmailCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Auth")
            .AllowAnonymous();

        return endpoints;
    }
}
