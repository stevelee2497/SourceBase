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

        public string GetUser()
        {
            return _httpContextAccessor.HttpContext.User?.Identity?.Name;
        }

        public string GetUserId()
        {
            return _httpContextAccessor.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task SignInAsync(ClaimsPrincipal user)
        {
            await _httpContextAccessor.HttpContext.SignInAsync(IdentityConstants.BearerScheme, user, new AuthenticationProperties() { IsPersistent = false });
        }
    }
}
