using Microsoft.AspNetCore.Identity;

namespace SourceBase.Domain.Entities;

public class RoleEntity : IdentityRole<Guid>
{
    public override string? Name { get; set; }

    public List<UserEntity> Users { get; set; } = [];
}
