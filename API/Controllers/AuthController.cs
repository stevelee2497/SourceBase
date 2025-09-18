using Application.Services;
using Domain.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task Register(RegisterRequest registration)
    {
        await authService.RegisterAsync(registration);
    }

    [HttpPost("login")]
    public async Task Login(LoginRequest login)
    {
        await authService.LoginAsync(login);
    }

    [HttpPost("refresh")]
    public async Task Refresh(RefreshTokenRequest login)
    {
        await authService.RefreshAsync(login);
    }

    [HttpGet("info")]
    [Authorize]
    public async Task<UserInfoResponse> GetUserInfo()
    {
        return await authService.GetUserInfoAsync();
    }

    [HttpPut("info")]
    [Authorize]
    public async Task<UserInfoResponse> UpdateUserInfo(UserInfoUpdateRequest userInfo)
    {
        return await authService.UpdateUserInfoAsync(userInfo);
    }
}
