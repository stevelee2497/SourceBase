using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId => User?.UserId ?? throw new UnAuthorizedException("User not authenticated");

    public string? Email => User?.Email;

    public string? UserName => User?.UserName;

    public string[] Roles => [.. User?.Roles ?? []];
}