using Microsoft.AspNetCore.Http;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using System.Security.Claims;

namespace SourceBase.Infrastructure.Identity;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId => Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new UnAuthorizedException();

    public string UserEmail => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "Un authorized user";
}
