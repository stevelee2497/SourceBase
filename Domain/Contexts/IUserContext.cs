namespace Domain.Contexts;

public interface IUserContext
{
    Guid CurrentUserId { get; }
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
    Task RegisterAsync(string email, string password);
}
