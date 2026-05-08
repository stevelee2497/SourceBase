using Microsoft.AspNetCore.Identity;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>, IBaseEntity
{
    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}
