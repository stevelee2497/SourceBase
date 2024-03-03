using System.Security.Claims;

namespace Core.Helpers
{
    public interface ISessionUserHelper
    {
        string UserId { get; }
        string User { get; }
        Task SignInAsync(ClaimsPrincipal user);
    }
}
