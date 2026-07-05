using System.Text.Json.Serialization;

namespace SourceBase.Domain.Entities;

public class MachineEntity : BaseAuditableEntity
{
    public required string Name { get; set; }

    public string? Alias { get; set; }

    public MachineStatus Status { get; set; }

    public DateTime? LastReportedOn { get; set; }

    public required Guid UserId { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MachineStatus
{
    Active,
    Inactive,
    Maintenance
}
