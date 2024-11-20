using Core.DTOs;

namespace Core.Contexts;

public interface IUserContext
{
    Guid GetCurrentUserId();
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
    Task RegisterAsync(RegisterRequestDto registration);
}