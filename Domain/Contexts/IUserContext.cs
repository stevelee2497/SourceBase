namespace Domain.Contexts;

public interface IUserContext
{
    Guid CurrentUserId { get; }

    Task ConfirmEmailAsync(string userId, string code);
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
    Task<string> RegisterAsync(string email, string password);
}
