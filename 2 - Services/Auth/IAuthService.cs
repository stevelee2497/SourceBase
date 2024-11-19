using Core.DTOs;

namespace Services.Auth
{
    public interface IAuthService
    {
        Task LoginAsync(LoginRequestDto login);
        Task RegisterAsync(RegisterRequestDto registration);
        Task RefreshAsync(RefreshTokenDto refreshToken);
        Task<UserInfoDto> GetUserInfoAsync();
        Task<UserInfoDto> UpdateUserInfoAsync(UserInfoDto userInfoDto);
    }
}
