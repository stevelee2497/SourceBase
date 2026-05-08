using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.DbContexts;
using System.Security.Claims;
using System.Text;

namespace SourceBase.Infrastructure.Identity;

public class IdentityContext(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
    ApplicationDbContext dbContext) : IIdentityContext
{
    public async Task CreateUserAsync(string email, string password)
    {
        var user = new ApplicationUser { Email = email, UserName = email };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new ApiInternalException(result.Errors.First().Description);
        }
    }

    public async Task ValidateAndSignInAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null || !await userManager.IsEmailConfirmedAsync(user))
        {
            throw new UnAuthorizedException("Invalid credentials");
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            throw new UnAuthorizedException("Invalid credentials");
        }

        await SignInAsync(user);
    }

    public async Task RefreshTokenAsync(string refreshToken)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(refreshToken);
        var user = await signInManager.ValidateSecurityStampAsync(refreshTicket?.Principal);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc || user == null)
        {
            throw new UnAuthorizedException("Invalid token");
        }

        await SignInAsync(user);
    }

    public async Task ConfirmEmailAsync(string email, string code, string role)
    {
        var user = await userManager.FindByEmailAsync(email) ?? throw new UnAuthorizedException();

        var decodedCode = Encoding.UTF8.GetString(Base64UrlHelper.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, decodedCode);
        if (!result.Succeeded)
        {
            throw new UnAuthorizedException();
        }

        await userManager.AddToRoleAsync(user, role);
    }

    public async Task ResetPasswordAsync(string email, string code, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email) ?? throw new NotFoundException("User not found");
        var decodedCode = Encoding.UTF8.GetString(Base64UrlHelper.Base64UrlDecode(code));
        var result = await userManager.ResetPasswordAsync(user, decodedCode, newPassword);
        if (!result.Succeeded)
        {
            throw new ApiInternalException(result.Errors.First().Description);
        }
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email) ?? throw new NotFoundException("User not found");
        if (user.EmailConfirmed)
        {
            throw new ApiInternalException("Email already confirmed");
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        return Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email) ?? throw new NotFoundException("User not found");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    private async Task SignInAsync(ApplicationUser user)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
    }

    public async Task<UserEntity?> GetUserWithRolesAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.Set<ApplicationUser>()
            .Where(u => u.Id == userId)
            .Select(u => new UserEntity
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Roles = dbContext.Set<IdentityUserRole<Guid>>()
                    .Where(ur => ur.UserId == u.Id)
                    .Join(dbContext.Set<ApplicationRole>(), ur => ur.RoleId, r => r.Id,
                        (ur, r) => new RoleEntity { Id = r.Id, Name = r.Name })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateUserInfoAsync(Guid userId, string? firstName, string? lastName, CancellationToken ct = default)
    {
        var user = await dbContext.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new NotFoundException();
        user.FirstName = firstName;
        user.LastName = lastName;
        await dbContext.SaveChangesAsync(ct);
    }
}
