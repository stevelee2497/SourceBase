namespace SourceBase.Domain.Entities;

public interface IBaseEntity
{
    string? CreatedBy { get; set; }
    DateTime? CreatedOn { get; set; }
    Guid Id { get; set; }
    string? UpdatedBy { get; set; }
    DateTime? UpdatedOn { get; set; }
}

public abstract class BaseEntity : IBaseEntity
{
    public Guid Id { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}