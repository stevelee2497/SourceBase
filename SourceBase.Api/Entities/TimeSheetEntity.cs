namespace SourceBase.Api.Entities;

public class TimeSheetEntity : BaseAuditableEntity
{
    public DateOnly Date { get; set; }

    public required string Project { get; set; }

    public decimal Hours { get; set; }

    public required Guid UserId { get; set; }
}
