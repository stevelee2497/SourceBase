using Core.DbContexts;
using Core.DTOs;
using Core.Exceptions;
using Core.Extensions;
using Services.Helpers;

namespace Services.Auth
{
    public class AuthService(IAuthHelper authHelper, IDbContext context) : IAuthService
    {
        public async Task Register(AuthRequestDto registration)
        {
            await authHelper.RegisterAsync(registration.Email, registration.Password);
        }

        public async Task Login(AuthRequestDto login)
        {
            await authHelper.LoginAsync(login.Email, login.Password);
        }

        public async Task Refresh(RefreshTokenDto refreshToken)
        {
            await authHelper.RefreshAsync(refreshToken.Token);
        }

        public async Task<UserInfoDto> GetUserInfo()
        {
            var userEntity = await context.Users.FindAsync(Guid.Parse(context.CurrentUserId)) ?? throw new SystemApiException("User not found");

            return userEntity.ToDto();
        }

        public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
        {
            var userEntity = await context.Users.FindAsync(Guid.Parse(context.CurrentUserId)) ?? throw new SystemApiException("User not found");

            userEntity.FirstName = userInfoDto.FirstName;
            userEntity.LastName = userInfoDto.LastName;

            await context.SaveChangesAsync();

            return userEntity.ToDto();
        }
    }
}
