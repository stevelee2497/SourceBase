namespace SourceBase.Api.Entities;

public interface IAuditableEntity
{
    Guid Id { get; set; }
    DateTime? CreatedOn { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedOn { get; set; }
    string? UpdatedBy { get; set; }
}

public abstract class BaseAuditableEntity : IAuditableEntity
{
    public Guid Id { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}