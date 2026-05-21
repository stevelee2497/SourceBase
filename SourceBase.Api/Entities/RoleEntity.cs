using Microsoft.AspNetCore.Identity;

namespace SourceBase.Api.Entities;

public class RoleEntity : IdentityRole<Guid>, IAuditableEntity
{
    public string? Description { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}
