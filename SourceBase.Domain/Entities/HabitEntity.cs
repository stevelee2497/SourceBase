namespace SourceBase.Domain.Entities;

public class HabitEntity : BaseAuditableEntity
{
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public Guid? UserId { get; set; }
    public bool IsSystem { get; set; }

    public List<HabitLogEntity> HabitLogs { get; set; } = [];
}
