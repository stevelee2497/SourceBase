using Core.Entities;
using Core.Exceptions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Services.Helpers;
using System.Net;
using System.Security.Claims;

namespace API.Helpers
{
    public class AuthHelper : IAuthHelper
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly SignInManager<UserEntity> _signInManager;
        private readonly IOptionsMonitor<BearerTokenOptions> _bearerTokenOptions;

        public AuthHelper(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _bearerTokenOptions = bearerTokenOptions;
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

        public async Task RefreshAsync(string refreshToken)
        {
            var refreshTokenProtector = _bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(refreshToken);
            var user = await _signInManager.ValidateSecurityStampAsync(refreshTicket?.Principal);

            if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc || user == null)
            {
                throw new UnAuthorizedException();
            }

            _signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            await _signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
        }
    }
}
