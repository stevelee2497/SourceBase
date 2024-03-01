using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<Ok> Register(AuthRequestDto registration)
        {
            var user = new IdentityUser() { Email = registration.Email, UserName = registration.Email };
            var result = await _signInManager.UserManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                throw new Exception(JsonSerializer.Serialize(result.Errors));
            }

            return TypedResults.Ok();
        }

        [HttpPost("login")]
        public async Task<EmptyHttpResult> Login(AuthRequestDto login)
        {
            _signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

            var result = await _signInManager.PasswordSignInAsync(login.Email, login.Password, false, true);

            if (!result.Succeeded)
            {
                throw new Exception(result.ToString());
            }

            // The signInManager already produced the needed response in the form of a bearer token.
            return TypedResults.Empty;
        }
    }
}
