namespace SourceBase.Domain.Entities;

public class RoleEntity : BaseEntity
{
    public string? Name { get; set; }

    public string? NormalizedName { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public List<UserEntity> Users { get; set; } = [];
}
