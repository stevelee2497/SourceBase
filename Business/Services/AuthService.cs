using Core.Contexts;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public interface IAuthService
{
    Task LoginAsync(LoginRequestDto login);
    Task RegisterAsync(RegisterRequestDto registration);
    Task RefreshAsync(RefreshTokenDto refreshToken);
    Task<UserInfoDto> GetUserInfoAsync();
    Task<UserInfoDto> UpdateUserInfoAsync(UserInfoDto userInfoDto);
}

public class AuthService(IUserContext userContext, IDbContext dbContext) : IAuthService
{
    public async Task RegisterAsync(RegisterRequestDto registration)
    {
        await userContext.RegisterAsync(registration);
    }

    public async Task LoginAsync(LoginRequestDto login)
    {
        await userContext.LoginAsync(login.Email, login.Password);
    }

    public async Task RefreshAsync(RefreshTokenDto refreshToken)
    {
        await userContext.RefreshAsync(refreshToken.Token);
    }

    public async Task<UserInfoDto> GetUserInfoAsync()
    {
        var userEntity = await dbContext.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == userContext.CurrentUserId) ?? throw new NotFoundException();
        return new UserInfoDto(userEntity.Id, userEntity.Email, userEntity.FirstName, userEntity.LastName, userEntity.PhoneNumber, [.. userEntity.Roles.Select(x => x.Name!)]);
    }

    public async Task<UserInfoDto> UpdateUserInfoAsync(UserInfoDto userInfoDto)
    {
        var userEntity = await dbContext.Users.FindAsync(userContext.CurrentUserId) ?? throw new NotFoundException();

        userEntity.FirstName = userInfoDto.FirstName;
        userEntity.LastName = userInfoDto.LastName;
        userEntity.PhoneNumber = userInfoDto.PhoneNumber;

        await dbContext.SaveChangesAsync();

        return await GetUserInfoAsync();
    }
}


public record LoginRequestDto(string Email, string Password);

public record RefreshTokenDto(string Token);

public record UserInfoDto(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);