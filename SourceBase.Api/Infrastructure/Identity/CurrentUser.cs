using System.Security.Claims;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Infrastructure.Identity;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId => Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId
        : throw new UnAuthorizedException();

    public string UserEmail => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "Un authorized user";
}