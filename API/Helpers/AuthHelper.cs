using Core.Entities;
using Core.Exceptions;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;
using System.Net;

namespace API.Helpers
{
    public class AuthHelper : IAuthHelper
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly SignInManager<UserEntity> _signInManager;

        public AuthHelper(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task LoginAsync(string email, string password)
        {
            _signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

            if (!result.Succeeded) 
            {
                throw new SystemApiException(result.ToString(), statusCode: (int)HttpStatusCode.Unauthorized);
            }
        }

        public async Task RegisterAsync(string email, string password)
        {
            var user = new UserEntity { Email = email, UserName = email };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                throw new SystemApiException(result.Errors.First().Description);
            }
        }
    }
}
