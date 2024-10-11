using Core.DTOs;

namespace Services.Auth
{
    public interface IAuthService
    {
        Task Login(AuthRequestDto login);
        Task Register(AuthRequestDto registration);
        Task Refresh(RefreshTokenDto refreshToken);
        Task<UserInfoDto> GetUserInfo();
        Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto);
    }
}
