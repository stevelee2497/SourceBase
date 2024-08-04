using Core.DbContexts;
using Core.Exceptions;
using Core.Extensions;
using Services.Helpers;

namespace Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IDbContext _context;
        private readonly IAuthHelper _authHelper;

        public AuthService(IAuthHelper authHelper, IDbContext context)
        {
            _context = context;
            _authHelper = authHelper;
        }

        public async Task Register(AuthRequestDto registration)
        {
            await _authHelper.RegisterAsync(registration.Email, registration.Password);
        }

        public async Task Login(AuthRequestDto login)
        {
            await _authHelper.LoginAsync(login.Email, login.Password);
        }

        public async Task<UserInfoDto> GetUserInfo()
        {
            var userEntity = await _context.Users.FindAsync(_context.CurrentUserId) ?? throw new SystemApiException("User not found");

            return userEntity.ToDto();
        }

        public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
        {
            var userEntity = await _context.Users.FindAsync(_context.CurrentUserId) ?? throw new SystemApiException("User not found");

            userEntity.FirstName = userInfoDto.FirstName;
            userEntity.LastName = userInfoDto.LastName;

            await _context.SaveChangesAsync();

            return userEntity.ToDto();
        }
    }
}
