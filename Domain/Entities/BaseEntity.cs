namespace Domain.Entities;

public interface IBaseEntity
{
    Guid Id { get; set; }
    string? CreatedBy { get; set; }
    DateTime? CreatedOn { get; set; }
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