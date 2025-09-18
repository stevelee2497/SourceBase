using Core.Contexts;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Business.Services;

public class AuthService(IUserContext userContext, IDbContext dbContext) : IAuthService
{
    public async Task RegisterAsync(RegisterRequest registration)
    {
        await userContext.RegisterAsync(registration);
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
        var userEntity = await dbContext.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == userContext.CurrentUserId) ?? throw new NotFoundException();
        return new UserInfoResponse(userEntity.Id, userEntity.Email, userEntity.FirstName, userEntity.LastName, userEntity.PhoneNumber, [.. userEntity.Roles.Select(x => x.Name!)]);
    }

    public async Task<UserInfoResponse> UpdateUserInfoAsync(UserInfoUpdateRequest userInfo)
    {
        var userEntity = await dbContext.Users.FindAsync(userContext.CurrentUserId) ?? throw new NotFoundException();

        userEntity.FirstName = userInfo.FirstName;
        userEntity.LastName = userInfo.LastName;
        userEntity.PhoneNumber = userInfo.PhoneNumber;

        await dbContext.SaveChangesAsync();

        return await GetUserInfoAsync();
    }
}

public interface IAuthService
{
    Task LoginAsync(LoginRequest login);
    Task RegisterAsync(RegisterRequest registration);
    Task RefreshAsync(RefreshTokenRequest refreshToken);
    Task<UserInfoResponse> GetUserInfoAsync();
    Task<UserInfoResponse> UpdateUserInfoAsync(UserInfoUpdateRequest userInfo);
}


public record LoginRequest([Required] string Email, [Required] string Password);

public record RefreshTokenRequest([Required] string Token);

public record UserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);

public record UserInfoUpdateRequest(string? FirstName, string? LastName, string? PhoneNumber);