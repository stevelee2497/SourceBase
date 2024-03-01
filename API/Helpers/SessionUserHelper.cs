using Core.Helpers;
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
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
        }

        public string GetUserId()
        {
            return _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
