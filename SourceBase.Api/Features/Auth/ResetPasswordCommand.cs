using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;

namespace SourceBase.Api.Features.Auth;

public record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var decodedCode = Encoding.UTF8.GetString(Base64UrlHelper.Base64UrlDecode(request.Code));
        var result = await userManager.ResetPasswordAsync(user, decodedCode, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ApiInternalException(result.Errors.First().Description);
        }
    }
}

public static class ResetPasswordCommandEndpoint
{
    public static IEndpointRouteBuilder MapResetPasswordCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/resetPassword", async (ResetPasswordCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Auth")
            .AllowAnonymous();

        return endpoints;
    }
}
