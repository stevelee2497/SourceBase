using Core.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace API.Helpers
{
    public class SessionUserHelper : ISessionUserHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionUserHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous user";

        public string User => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? "Anonymous user";

        public Task SignInAsync(ClaimsPrincipal user)
        {
            return _httpContextAccessor.HttpContext?.SignInAsync(IdentityConstants.BearerScheme, user) ?? Task.CompletedTask;
        }
    }
}
