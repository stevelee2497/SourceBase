using Core.Contexts;
using Core.Entities;
using Core.Exceptions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Core.DTOs;

namespace API.Contexts
{
    public class UserContext(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions) : IUserContext
    {
        public async Task LoginAsync(string email, string password)
        {
            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            var result = await signInManager.PasswordSignInAsync(email, password, false, false);

            if (!result.Succeeded)
            {
                throw new UnAuthorizedException("Invalid credentials");
            }
        }

        public async Task RegisterAsync(RegisterRequestDto registration)
        {
            // Create a new user
            var user = new UserEntity
            {
                Email = registration.Email,
                UserName = registration.Email,
                PhoneNumber = registration.PhoneNumber
            };
            var result = await userManager.CreateAsync(user, registration.Password);
            if (!result.Succeeded)
            {
                throw new SystemApiException(result.Errors.First().Description);
            }

            // Assign role to the user
            result = await userManager.AddToRoleAsync(user, registration.Role);
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

            if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc || user == null)
            {
                throw new UnAuthorizedException("Invalid token");
            }

            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
        }
    }
}
