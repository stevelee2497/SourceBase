using Microsoft.AspNetCore.Identity;

namespace SourceBase.Api.Entities;

public class UserRoleEntity : IdentityUserRole<Guid>
{
    public UserEntity User { get; set; } = null!;

    public RoleEntity Role { get; set; } = null!;
}
