namespace Services.Helpers
{
    public interface IAuthHelper
    {
        Task LoginAsync(string email, string password);
        Task RefreshAsync(string refreshToken);
        Task RegisterAsync(string email, string password);
    }
}
