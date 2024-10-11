using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public class AuthRequestDto
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
