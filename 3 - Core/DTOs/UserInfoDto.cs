namespace Core.DTOs
{
    public class UserInfoDto
    {
        public Guid Id { get; init; }

        public string? Email { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }
        
        public string? PhoneNumber { get; init; }

        public string[] Roles { get; init; } = [];
    }
}
