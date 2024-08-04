namespace Services.Helpers
{
    public interface IAuthHelper
    {
        Task LoginAsync(string email, string password);
        Task RegisterAsync(string email, string password);
    }
}
