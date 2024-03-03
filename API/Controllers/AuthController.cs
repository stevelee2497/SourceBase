using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services;

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
        public async Task<Ok> Register(AuthRequestDto registration)
        {
            await _authService.Register(registration);

            return TypedResults.Ok();
        }

        [HttpPost("login")]
        public async Task<EmptyHttpResult> Login(AuthRequestDto login)
        {
            await _authService.Login(login);

            return TypedResults.Empty;
        }

        [HttpGet("info")]
        [Authorize]
        public async Task<UserInfoDto> GetUserInfo()
        {
            return await _authService.GetUserInfo(User);
        }
    }
}
