using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Features.Auth;

namespace SourceBase.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public Task Register(RegisterRequest registration)
    {
        return authService.RegisterAsync(registration);
    }

    [HttpPost("login")]
    public Task Login(LoginRequest login)
    {
        return authService.LoginAsync(login);
    }

    [HttpPost("confirmEmail")]
    public Task ConfirmEmail(ConfirmEmailRequest request)
    {
        return authService.ConfirmEmailAsync(request);
    }

    [HttpPost("refresh")]
    public Task Refresh(RefreshTokenRequest login)
    {
        return authService.RefreshAsync(login);
    }

    [HttpPost("forgotPassword")]
    public Task ForgotPassword(ForgotPasswordRequest request)
    {
        return authService.ForgotPasswordAsync(request);
    }

    [HttpPost("resendConfirmationEmail")]
    public Task ResendConfirmationEmail(ResendConfirmationEmailRequest request)
    {
        return authService.ResendConfirmationEmailAsync(request);
    }

    [HttpPost("resetPassword")]
    public Task ResetPassword(ResetPasswordRequest request)
    {
        return authService.ResetPasswordAsync(request);
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
