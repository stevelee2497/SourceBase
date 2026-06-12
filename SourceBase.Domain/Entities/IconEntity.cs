using System.Text.Json.Serialization;

namespace SourceBase.Domain.Entities;

public class IconEntity : BaseAuditableEntity
{
    public required string Value { get; set; }

    public required string Name { get; set; }

    public IconGroup Group { get; set; }

    public int SortOrder { get; set; }

    public bool IsSystem { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IconGroup
{
    Wallet,
    Category,
    General
}
