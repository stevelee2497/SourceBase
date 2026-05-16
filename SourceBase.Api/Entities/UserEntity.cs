using Microsoft.AspNetCore.Identity;

namespace SourceBase.Api.Entities;

public class UserEntity : IdentityUser<Guid>, IBaseEntity
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}
