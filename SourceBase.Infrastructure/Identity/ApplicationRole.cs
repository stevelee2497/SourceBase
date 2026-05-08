using Microsoft.AspNetCore.Identity;

namespace SourceBase.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}
