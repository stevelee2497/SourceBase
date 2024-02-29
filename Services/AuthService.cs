using Core.Helpers;

namespace Services
{
    public interface IAuthService
    {
        Task Login(AuthRequestDto login);
        Task Register(AuthRequestDto registration);
    }

    public class AuthService : IAuthService
    {
        private readonly ISessionUserHelper sessionUserHelper;

        public AuthService(ISessionUserHelper sessionUserHelper)
        {
            this.sessionUserHelper = sessionUserHelper;
        }

        public async Task Register(AuthRequestDto registration)
        {
            throw new NotImplementedException();
        }

        public async Task Login(AuthRequestDto login)
        {
            throw new NotImplementedException();
        }
    }
}
