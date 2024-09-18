using Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Auth;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task Register(AuthRequestDto registration)
        {
            await authService.Register(registration);
        }

        [HttpPost("login")]
        public async Task Login(AuthRequestDto login)
        {
            await authService.Login(login);
        }

        [HttpPost("refresh")]
        public async Task Refresh(RefreshTokenDto login)
        {
            await authService.Refresh(login);
        }

        [HttpGet("info")]
        [Authorize]
        public async Task<UserInfoDto> GetUserInfo()
        {
            return await authService.GetUserInfo();
        }

        [HttpPost("info")]
        [Authorize]
        public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
        {
            return await authService.UpdateUserInfo(userInfoDto);
        }
    }
}
