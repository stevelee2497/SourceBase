using Core.DTOs;

namespace Core.Contexts;

public interface IUserContext
{
    Guid CurrentUserId { get; }
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
    Task RegisterAsync(RegisterRequestDto registration);
}