using Microsoft.AspNetCore.Identity;

namespace SourceBase.Api.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>, IBaseEntity
{
    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}
