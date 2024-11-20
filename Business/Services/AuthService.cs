using Business.Interfaces;
using Core.Contexts;
using Core.DTOs;
using Core.Exceptions;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
{
    public class AuthService(IUserContext userContext, IDbContext dbContext) : IAuthService
    {
        public async Task RegisterAsync(RegisterRequestDto registration)
        {
            await userContext.RegisterAsync(registration);
        }

        public async Task LoginAsync(LoginRequestDto login)
        {
            await userContext.LoginAsync(login.Email, login.Password);
        }

        public async Task RefreshAsync(RefreshTokenDto refreshToken)
        {
            await userContext.RefreshAsync(refreshToken.Token);
        }

        public async Task<UserInfoDto> GetUserInfoAsync()
        {
            var userId = dbContext.GetCurrentUserId() ?? throw new UnAuthorizedException();
            var userEntity = await dbContext.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == userId) ?? throw new NotFoundException();
            return userEntity.ToDto();
        }

        public async Task<UserInfoDto> UpdateUserInfoAsync(UserInfoDto userInfoDto)
        {
            var userEntity = await dbContext.Users.FindAsync(dbContext.GetCurrentUserId()) ?? throw new NotFoundException();

            userEntity.FirstName = userInfoDto.FirstName;
            userEntity.LastName = userInfoDto.LastName;
            userEntity.PhoneNumber = userInfoDto.PhoneNumber;

            await dbContext.SaveChangesAsync();

            return await GetUserInfoAsync();
        }
    }
}
