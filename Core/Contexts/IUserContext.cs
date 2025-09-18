using System.ComponentModel.DataAnnotations;

namespace Core.Contexts;

public interface IUserContext
{
    Guid CurrentUserId { get; }
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
    Task RegisterAsync(RegisterRequest registration);
}

public record RegisterRequest([Required] string Email, [Required] string Password, [Required] string Role, string? FirstName, string? LastName, string? PhoneNumber);