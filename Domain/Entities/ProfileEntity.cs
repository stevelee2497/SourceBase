namespace Domain.Entities;

public class ProfileEntity : BaseEntity
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public Guid UserId { get; set; }

    public UserEntity UserEntity { get; set; } = null!;
}