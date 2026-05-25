using System.Security.Claims;
using SourceBase.Api.Entities;

namespace SourceBase.Api.Shared.Interfaces;

public interface IClaimsManager
{
    Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(UserEntity user, IEnumerable<Claim>? additionalClaims = null);
}
