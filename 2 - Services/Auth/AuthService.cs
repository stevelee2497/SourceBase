using Core.Contexts;
using Core.DTOs;
using Core.Exceptions;
using Core.Extensions;

namespace Services.Auth
{
    public class AuthService(IUserContext userContext, IDbContext dbContext) : IAuthService
    {
        public async Task Register(AuthRequestDto registration)
        {
            await userContext.RegisterAsync(registration.Email, registration.Password);
        }

        public async Task Login(AuthRequestDto login)
        {
            await userContext.LoginAsync(login.Email, login.Password);
        }

        public async Task Refresh(RefreshTokenDto refreshToken)
        {
            await userContext.RefreshAsync(refreshToken.Token);
        }

        public async Task<UserInfoDto> GetUserInfo()
        {
            var userEntity = await dbContext.Users.FindAsync(dbContext.GetCurrentUserId()) ?? throw new NotFoundException();

            return userEntity.ToDto();
        }

        public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
        {
            var userEntity = await dbContext.Users.FindAsync(dbContext.GetCurrentUserId()) ?? throw new NotFoundException();

            userEntity.FirstName = userInfoDto.FirstName;
            userEntity.LastName = userInfoDto.LastName;

            await dbContext.SaveChangesAsync();

            return userEntity.ToDto();
        }
    }
}
