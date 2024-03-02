using System.ComponentModel.DataAnnotations;

namespace Services
{
    public class AuthRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; }

        [Required]
        public string Password { get; init; }
    }

    public class UserInfoDto
    {
        public Guid Id { get; init; }

        public string Email { get; init; }
    }
}
