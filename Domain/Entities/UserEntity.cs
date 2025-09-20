using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class UserEntity : IdentityUser<Guid>
{
    public List<RoleEntity> Roles { get; set; } = [];

    public Guid ProfileId { get; set; }

    public ProfileEntity Profile { get; set; } = null!;
}
