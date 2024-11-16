namespace Core.Contexts
{
    public interface IUserContext
    {
        Task LoginAsync(string email, string password);
        Task RefreshAsync(string refreshToken);
        Task RegisterAsync(string email, string password);
    }
}
