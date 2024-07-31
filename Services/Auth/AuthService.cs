using Core.DbContexts;
using Core.Entities;
using Core.Exceptions;
using Core.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Core.Extensions;

namespace Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<UserEntity> _userManager;
        private readonly ISessionUserHelper _sessionUserHelper;
        private readonly IUserClaimsPrincipalFactory<UserEntity> _claimsFactory;

        public AuthService(UserManager<UserEntity> userManager, IUserClaimsPrincipalFactory<UserEntity> claimsFactory, ISessionUserHelper sessionUserHelper, ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _sessionUserHelper = sessionUserHelper;
        }

        public async Task Register(AuthRequestDto registration)
        {
            var user = new UserEntity { Email = registration.Email, UserName = registration.Email };
            var result = await _userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                throw new SystemApiException(result.Errors.First().Description);
            }
        }

        public async Task Login(AuthRequestDto login)
        {
            var user = await _userManager.FindByNameAsync(login.Email);
            if (user == null)
            {
                throw new SystemApiException("User not found");
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, login.Password);
            if (validPassword == false)
            {
                throw new SystemApiException("Invalid password");
            }

            var userPrincipal = await _claimsFactory.CreateAsync(user);
            foreach (var claim in new Claim[] { new("amr", "pwd") })
            {
                userPrincipal.Identities.First().AddClaim(claim);
            }

            await _sessionUserHelper.SignInAsync(userPrincipal);
        }

        public async Task<UserInfoDto> GetUserInfo()
        {
            var userEntity = await _context.Users.FindAsync(Guid.Parse(_sessionUserHelper.UserId)) ?? throw new SystemApiException("User not found");

            return userEntity.ToDto();
        }

        public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
        {
            var userEntity = await _context.Users.FindAsync(Guid.Parse(_sessionUserHelper.UserId)) ?? throw new SystemApiException("User not found");

            userEntity.FirstName = userInfoDto.FirstName;
            userEntity.LastName = userInfoDto.LastName;

            await _context.SaveChangesAsync();

            return userEntity.ToDto();
        }
    }
}
