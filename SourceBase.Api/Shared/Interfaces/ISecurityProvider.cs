using System.Security.Claims;
using SourceBase.Api.Entities;

namespace SourceBase.Api.Shared.Interfaces;

public interface ISecurityProvider
{
    ClaimsPrincipal CreateClaimsPrincipal(UserEntity user, IEnumerable<Claim>? additionalClaims = null);

    ClaimsPrincipal ParseRefreshToken(string token);

    string HashPassword(UserEntity user, string password);

    bool VerifyPassword(UserEntity user, string password);
}
