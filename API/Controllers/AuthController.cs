using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.Auth;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task Register(AuthRequestDto registration)
        {
            await _authService.Register(registration);
        }

        [HttpPost("login")]
        public async Task Login(AuthRequestDto login)
        {
            await _authService.Login(login);
        }

        [HttpGet("info")]
        [Authorize]
        public async Task<UserInfoDto> GetUserInfo()
        {
            return await _authService.GetUserInfo();
        }

        [HttpPost("info")]
        [Authorize]
        public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
        {
            return await _authService.UpdateUserInfo(userInfoDto);
        }
    }
}
