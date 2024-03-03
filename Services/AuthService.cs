using Core.Entities;
using Core.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;

namespace Services
{
    public interface IAuthService
    {
        Task Login(AuthRequestDto login);
        Task Register(AuthRequestDto registration);
        Task<UserInfoDto> GetUserInfo(ClaimsPrincipal User);
    }

    public class AuthService : IAuthService
    {
        private readonly ISessionUserHelper _sessionUserHelper;
        private readonly UserManager<UserEntity> _userManager;
        private readonly IUserClaimsPrincipalFactory<UserEntity> _claimsFactory;

        public AuthService(UserManager<UserEntity> userManager, IUserClaimsPrincipalFactory<UserEntity> claimsFactory, ISessionUserHelper sessionUserHelper)
        {
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
                throw new Exception(JsonSerializer.Serialize(result.Errors));
            }
        }

        public async Task Login(AuthRequestDto login)
        {
            var user = await _userManager.FindByNameAsync(login.Email);
            if (user == null)
            {
                throw new Exception(SignInResult.Failed.ToString());
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, login.Password);
            if (validPassword == false)
            {
                throw new Exception(SignInResult.Failed.ToString());
            }

            var userPrincipal = await _claimsFactory.CreateAsync(user);
            foreach (var claim in new Claim[] { new Claim("amr", "pwd") })
            {
                userPrincipal.Identities.First().AddClaim(claim);
            }

            await _sessionUserHelper.SignInAsync(userPrincipal);
        }

        public async Task<UserInfoDto> GetUserInfo(ClaimsPrincipal user)
        {
            var userInfo = await _userManager.GetUserAsync(user);

            return new UserInfoDto { Id = userInfo.Id, Email = userInfo.Email };
        }
    }
}
