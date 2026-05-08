using Microsoft.AspNetCore.Identity;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>, IBaseEntity
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}
