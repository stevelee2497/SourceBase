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

        public string UserId => _httpContextAccessor.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string User => _httpContextAccessor.HttpContext.User?.FindFirstValue(ClaimTypes.Name);

        public async Task SignInAsync(ClaimsPrincipal user)
        {
            await _httpContextAccessor.HttpContext.SignInAsync(IdentityConstants.BearerScheme, user, new AuthenticationProperties() { IsPersistent = false });
        }
    }
}
