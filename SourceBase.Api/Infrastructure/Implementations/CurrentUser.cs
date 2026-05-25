using System.Security.Claims;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.Implementations;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId => Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId
        : throw new UnAuthorizedException();

    public string UserEmail => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? "Un authorized user";

    public string UserName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "Un authorized user";

    public string[] Roles => httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray() ?? [];
}