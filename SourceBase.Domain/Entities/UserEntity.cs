namespace SourceBase.Domain.Entities;

public class UserEntity : BaseAuditableEntity
{
    public required string UserName { get; set; }

    public string? Email { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public required string SecurityStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? AvatarUrl { get; set; }

    public Guid? DefaultTodoListId { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpCodeExpiresOn { get; set; }

    public virtual ICollection<RoleEntity> Roles { get; set; } = [];
}
