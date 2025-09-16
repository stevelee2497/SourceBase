using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class UserEntity : IdentityUser<Guid>, IBaseEntity
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public override string? UserName { get; set; }

    public override string? Email { get; set; }

    public override string? PhoneNumber { get; set; }

    public List<RoleEntity> Roles { get; set; } = [];

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}