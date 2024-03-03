namespace Services.Auth
{
    public interface IAuthService
    {
        Task Login(AuthRequestDto login);
        Task Register(AuthRequestDto registration);
        Task<UserInfoDto> GetUserInfo();
        Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto);
    }
}
