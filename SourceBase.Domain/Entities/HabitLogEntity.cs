using System.Text.Json.Serialization;

namespace SourceBase.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HabitLogAction { HabitStarted, Dismissed, Snoozed, SuppressedVideo }

public class HabitLogEntity : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? HabitId { get; set; }
    public string? HabitName { get; set; }
    public HabitLogAction Action { get; set; }
    public DateTime OccurredAt { get; set; }

    public HabitEntity? Habit { get; set; }
}
