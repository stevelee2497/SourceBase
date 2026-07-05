using System.Text.Json.Serialization;

namespace SourceBase.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MachineCommandType
{
    Shutdown,
    Restart
}
