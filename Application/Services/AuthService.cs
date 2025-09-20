using Domain.Contexts;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Application.Services;

public class AuthService(IUserContext userContext, IDbContext dbContext) : IAuthService
{
    public async Task<string> RegisterAsync(RegisterRequest registration)
    {
        return await userContext.RegisterAsync(registration.Email, registration.Password);
    }

    public async Task LoginAsync(LoginRequest login)
    {
        await userContext.LoginAsync(login.Email, login.Password);
    }

    public async Task RefreshAsync(RefreshTokenRequest refreshToken)
    {
        await userContext.RefreshAsync(refreshToken.Token);
    }

    public async Task<UserInfoResponse> GetUserInfoAsync()
    {
        return await dbContext.Users
            .Include(x => x.Roles)
            .Include(x => x.Profile)
            .Where(x => x.Id == userContext.CurrentUserId)
            .Select(x => new UserInfoResponse(x.Id, x.Email, x.Profile.FirstName, x.Profile.LastName, x.PhoneNumber, x.Roles.Select(ur => ur.Name!)))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException();
    }

    public async Task UpdateUserInfoAsync(UserInfoUpdateRequest userInfo)
    {
        var profile = await dbContext.Profiles.FirstOrDefaultAsync(x => x.UserId == userContext.CurrentUserId) ?? throw new NotFoundException();

        profile.FirstName = userInfo.FirstName;
        profile.LastName = userInfo.LastName;

        await dbContext.SaveChangesAsync();
    }

    public Task ConfirmEmailAsync(string userId, string code)
    {
        return userContext.ConfirmEmailAsync(userId, code);
    }
}

public interface IAuthService
{
    Task LoginAsync(LoginRequest login);
    Task<string> RegisterAsync(RegisterRequest registration);
    Task RefreshAsync(RefreshTokenRequest refreshToken);
    Task<UserInfoResponse> GetUserInfoAsync();
    Task UpdateUserInfoAsync(UserInfoUpdateRequest userInfo);
    Task ConfirmEmailAsync(string userId, string code);
}

public record RegisterRequest([Required][EmailAddress] string Email, [Required] string Password);

public record LoginRequest([Required] string Email, [Required] string Password);

public record RefreshTokenRequest([Required] string Token);

public record UserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);

public record UserInfoUpdateRequest(string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);