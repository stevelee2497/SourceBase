using Microsoft.AspNetCore.Identity;

namespace SourceBase.Domain.Entities;

public class UserEntity : IdentityUser<Guid>
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public List<RoleEntity> Roles { get; set; } = [];
}
