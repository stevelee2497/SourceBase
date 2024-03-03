using System.Security.Claims;

namespace Services.Auth
{
    public interface IAuthService
    {
        Task Login(AuthRequestDto login);
        Task Register(AuthRequestDto registration);
        Task<UserInfoDto> GetUserInfo(ClaimsPrincipal User);
    }
}
