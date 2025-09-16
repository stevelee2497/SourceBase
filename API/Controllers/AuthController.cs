using Business.Services;
using Core.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task Register(RegisterRequestDto registration)
    {
        await authService.RegisterAsync(registration);
    }

    [HttpPost("login")]
    public async Task Login(LoginRequestDto login)
    {
        await authService.LoginAsync(login);
    }

    [HttpPost("refresh")]
    public async Task Refresh(RefreshTokenDto login)
    {
        await authService.RefreshAsync(login);
    }

    [HttpGet("info")]
    [Authorize]
    public async Task<UserInfoDto> GetUserInfo()
    {
        return await authService.GetUserInfoAsync();
    }

    [HttpPost("info")]
    [Authorize]
    public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
    {
        return await authService.UpdateUserInfoAsync(userInfoDto);
    }
}
