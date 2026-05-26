using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.Implementations;

public class SecurityProvider(IOptionsMonitor<BearerTokenOptions> bearerTokenOptions, IPasswordHasher<UserEntity> passwordHasher) : ISecurityProvider
{
    public ClaimsPrincipal CreateClaimsPrincipal(UserEntity user, IEnumerable<Claim>? additionalClaims = null)
    {
        var identity = new ClaimsIdentity(
            Constants.BearerScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        List<Claim> claims = [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(Constants.SecurityStampClaimType, user.SecurityStamp ?? string.Empty),
            ..user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name ?? string.Empty)),
            ..additionalClaims ?? []
        ];

        identity.AddClaims(claims);

        return new ClaimsPrincipal(identity);
    }

    public ClaimsPrincipal ParseRefreshToken(string token)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(Constants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(token);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc)
            throw new UnAuthorizedException("Invalid token");

        return refreshTicket.Principal ?? throw new UnAuthorizedException("Invalid token");
    }

    public string HashPassword(UserEntity user, string password)
    {
        return passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(UserEntity user, string password)
    {
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
