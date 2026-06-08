namespace SourceBase.Domain.Entities;

public class RoleEntity : BaseAuditableEntity
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public virtual ICollection<UserEntity> Users { get; set; } = [];
}
