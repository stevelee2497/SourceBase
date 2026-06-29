namespace SourceBase.Domain.Entities;

public enum HabitLogAction { HabitStarted, Dismissed, Snoozed, SuppressedVideo }

public class HabitLogEntity : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string? HabitId { get; set; }
    public string? HabitName { get; set; }
    public HabitLogAction Action { get; set; }
    public DateTime OccurredAt { get; set; }
}
