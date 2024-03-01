using Core.Helpers;

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
    }
}
