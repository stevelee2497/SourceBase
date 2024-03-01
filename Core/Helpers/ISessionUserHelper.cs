using System.Security.Claims;

namespace Core.Helpers
{
    public interface ISessionUserHelper
    {
        string GetUser();
        string GetUserId();
        Task SignInAsync(ClaimsPrincipal user);
    }
}
