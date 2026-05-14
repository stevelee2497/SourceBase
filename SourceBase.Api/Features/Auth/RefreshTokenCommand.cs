using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;

namespace SourceBase.Api.Features.Auth;

public record RefreshTokenCommand(string Token) : IRequest;

public class RefreshTokenCommandHandler(SignInManager<ApplicationUser> signInManager, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions) : IRequestHandler<RefreshTokenCommand>
{
    public async Task Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(request.Token);
        var user = await signInManager.ValidateSecurityStampAsync(refreshTicket?.Principal);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc || user == null)
        {
            throw new UnAuthorizedException("Invalid token");
        }

        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
    }
}

public static class RefreshTokenCommandEndpoint
{
    public static IEndpointRouteBuilder MapRefreshTokenCommandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
            })
            .WithTags("Auth")
            .AllowAnonymous();

        return endpoints;
    }
}
