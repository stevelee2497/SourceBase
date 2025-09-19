using Domain.Constants;
using Domain.Contexts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Api.Contexts;

// TODO: Move to infrastructure layer 
public class UserContext(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions, IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid CurrentUserId => Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new UnAuthorizedException();

    public async Task LoginAsync(string email, string password)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        // After this call, EF Identity will sign in the user to the http context and will return the access token to the API response after the request is executed
        // Noted that we can not get the access token directly, it is bind and only return after the request successfully executed
        var result = await signInManager.PasswordSignInAsync(email, password, false, true);

        if (!result.Succeeded)
        {
            throw new UnAuthorizedException("Invalid credentials");
        }
    }

    public async Task RegisterAsync(string email, string password)
    {
        // Create a new user
        var user = new UserEntity
        {
            Email = email,
            UserName = email
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new SystemApiException(result.Errors.First().Description);
        }

        // Assign role to the user
        result = await userManager.AddToRoleAsync(user, Roles.User);
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
