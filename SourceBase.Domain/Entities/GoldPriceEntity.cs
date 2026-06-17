using System.Text.Json.Serialization;

namespace SourceBase.Domain.Entities;

public class GoldPriceEntity : BaseAuditableEntity
{
    public GoldSource Source { get; set; }

    public decimal BuyPrice { get; set; }

    public decimal SellPrice { get; set; }

    public DateTime RecordedAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GoldSource
{
    SJC,
    PNJ,
    GiaVang,
    KimKhanhVietHung,
    NgocThinh
}
