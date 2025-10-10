using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;
using System.Security.Claims;

namespace SourceBase.Api.Contexts;

[ScopedDependency<IIdentityContext>]
public class IdentityContext(SignInManager<UserEntity> signInManager, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions) : IIdentityContext
{
    public async Task GenerateTokenAsync(UserEntity user)
    {
        // After this call, EF Identity will sign in the user to the http context and will return the access token to the API response after the request is executed
        // Noted that we can not get the access token directly, it is bind and only return after the request successfully executed
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
    }

    public async Task RefreshTokenAsync(string refreshToken)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(refreshToken);
        var user = await signInManager.ValidateSecurityStampAsync(refreshTicket?.Principal);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc || user == null)
        {
            throw new UnAuthorizedException("Invalid token");
        }

        await GenerateTokenAsync(user);
    }
}