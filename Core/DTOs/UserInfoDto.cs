namespace Services
{
    public class UserInfoDto
    {
        public Guid Id { get; init; }

        public required string Email { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }
    }
}
