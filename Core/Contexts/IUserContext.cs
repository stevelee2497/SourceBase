namespace Core.Contexts;

public interface IUserContext
{
    Guid CurrentUserId { get; }
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
    Task RegisterAsync(RegisterRequestDto registration);
}

public record RegisterRequestDto(string Email, string Password, string Role, string? FirstName, string? LastName, string? PhoneNumber);