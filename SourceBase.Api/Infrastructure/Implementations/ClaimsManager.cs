using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.Implementations;

public class ClaimsManager : IClaimsManager
{
    public Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(UserEntity user, IEnumerable<Claim>? additionalClaims = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var identity = new ClaimsIdentity(
            IdentityConstants.BearerScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        if (user.Id != Guid.Empty)
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

        if (!string.IsNullOrWhiteSpace(user.UserName))
            identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));

        if (!string.IsNullOrWhiteSpace(user.Email))
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));

        if (!string.IsNullOrWhiteSpace(user.SecurityStamp))
            identity.AddClaim(new Claim(Constants.SecurityStampClaimType, user.SecurityStamp));

        foreach (var roleName in user.Roles
                     .Select(role => role.Name)
                     .OfType<string>()
                     .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                     .Distinct(StringComparer.Ordinal))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        }

        if (additionalClaims is not null)
            identity.AddClaims(additionalClaims);

        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}
