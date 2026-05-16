using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Utilities;

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

public class RefreshTokenCommandEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/auth/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
            })
            .WithTags("Auth")
            .AllowAnonymous();
}
