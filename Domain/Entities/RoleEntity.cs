using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class RoleEntity : IdentityRole<Guid>, IBaseEntity
{
    public override string? Name { get; set; }

    public List<UserEntity> Users { get; set; } = [];

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}