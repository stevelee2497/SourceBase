using System.ComponentModel.DataAnnotations;

namespace Services
{
    public class AuthRequestDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; init; }

        [Required]
        public required string Password { get; init; }
    }
}
