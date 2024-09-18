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
    public class AuthHelper(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions) : IAuthHelper
    {
        public async Task LoginAsync(string email, string password)
        {
            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            var result = await signInManager.PasswordSignInAsync(email, password, false, false);

            if (!result.Succeeded)
            {
                throw new SystemApiException(result.ToString(), statusCode: (int)HttpStatusCode.Unauthorized);
            }
        }

        public async Task RegisterAsync(string email, string password)
        {
            var user = new UserEntity { Email = email, UserName = email };
            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                throw new SystemApiException(result.Errors.First().Description);
            }
        }

        public async Task RefreshAsync(string refreshToken)
        {
            var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(refreshToken);
            var user = await signInManager.ValidateSecurityStampAsync(refreshTicket?.Principal);

            if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc || user == null)
            {
                throw new UnAuthorizedException();
            }

            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
        }
    }
}
