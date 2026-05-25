using System.Security.Claims;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.Implementations;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId => User?.UserId ?? throw new UnAuthorizedException();

    public string UserName => User?.UserName ?? "Unknown";

    public string[] Roles => User?.Roles ?? [];
}