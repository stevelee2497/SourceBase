using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Features.Auth;

namespace SourceBase.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public Task Register([FromBody] RegisterCommand command)
    {
        return sender.Send(command);
    }

    [HttpPost("login")]
    public Task Login([FromBody] LoginCommand command)
    {
        return sender.Send(command);
    }

    [HttpPost("confirmEmail")]
    public Task ConfirmEmail([FromBody] ConfirmEmailCommand command)
    {
        return sender.Send(command);
    }

    [HttpPost("refresh")]
    public Task Refresh([FromBody] RefreshTokenCommand command)
    {
        return sender.Send(command);
    }

    [HttpPost("forgotPassword")]
    public Task ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        return sender.Send(command);
    }

    [HttpPost("resendConfirmationEmail")]
    public Task ResendConfirmationEmail([FromBody] ResendConfirmationEmailCommand command)
    {
        return sender.Send(command);
    }

    [HttpPost("resetPassword")]
    public Task ResetPassword([FromBody] ResetPasswordCommand command)
    {
        return sender.Send(command);
    }

    [HttpGet("info")]
    [Authorize]
    public Task<UserInfoResponse> GetUserInfo([FromQuery] GetUserInfoQuery query)
    {
        return sender.Send(query);
    }

    [HttpPut("info")]
    [Authorize]
    public Task UpdateUserInfo([FromBody] UpdateUserInfoCommand command)
    {
        return sender.Send(command);
    }
}
