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
        public string Id { get; init; }

        public string Email { get; init; }
    }
}
