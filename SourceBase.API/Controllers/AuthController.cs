using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Features.Auth;

namespace SourceBase.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public Task<string> Register(RegisterRequest registration)
    {
        return authService.RegisterAsync(registration);
    }

    [HttpPost("login")]
    public Task Login(LoginRequest login)
    {
        return authService.LoginAsync(login);
    }

    [HttpGet("confirmEmail", Name = "ConfirmEmail")]
    public Task ConfirmEmail(string userId, string code)
    {
        return authService.ConfirmEmailAsync(userId, code);
    }

    [HttpPost("refresh")]
    public Task Refresh(RefreshTokenRequest login)
    {
        return authService.RefreshAsync(login);
    }

    [HttpGet("info")]
    [Authorize]
    public Task<UserInfoResponse> GetUserInfo()
    {
        return authService.GetUserInfoAsync();
    }

    [HttpPut("info")]
    [Authorize]
    public Task UpdateUserInfo(UserInfoUpdateRequest userInfo)
    {
        return authService.UpdateUserInfoAsync(userInfo);
    }
}
