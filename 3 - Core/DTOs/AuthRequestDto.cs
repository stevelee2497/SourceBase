using System.ComponentModel.DataAnnotations;
using Core.Constants;

namespace Core.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; init; }

        [Required]
        public required string Password { get; init; }
        
        public string? PhoneNumber { get; init; }

        [RegularExpression($"^({Roles.Admin}|{Roles.User})$", ErrorMessage = "The role must be either 'Admin' or 'User'.")]
        public string Role { get; set; } = Roles.User;
    }
    
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; init; }

        [Required]
        public required string Password { get; init; }
    }

    public class RefreshTokenDto
    {
        public required string Token { get; init; }
    }
}
