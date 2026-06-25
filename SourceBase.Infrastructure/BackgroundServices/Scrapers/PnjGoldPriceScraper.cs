using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.BackgroundServices.Scrapers;

public class PnjGoldPriceScraper(ILogger<PnjGoldPriceScraper> logger) : IGoldPriceScraper
{
    public GoldSource Source => GoldSource.PNJ;

    public string Url => "https://edge-cf-api.pnj.io/ecom-frontend/v1/get-gold-price?zone=00";

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string jsonData, CancellationToken ct)
    {
        var response = jsonData.Deserialize<PnjGoldPriceResponse>();
        if (response is null || response.Data is null || response.Data.Count == 0)
        {
            logger.LogWarning("PNJ: empty or invalid response");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        var item = response.Data.FirstOrDefault(i => i.Masp.Equals("N24K", StringComparison.OrdinalIgnoreCase));
        return item is null || item.Giamua is null || item.Giaban is null
            ? Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null)
            : Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>((item.Giamua.Value * 1000, item.Giaban.Value * 1000));
    }
}

public record PnjGoldPriceResponse(List<PnjGoldPriceItem> Data);

public record PnjGoldPriceItem(
    string Masp,
    [property: JsonConverter(typeof(PnjDecimalConverter))] decimal? Giamua,
    [property: JsonConverter(typeof(PnjDecimalConverter))] decimal? Giaban);

// Some entries use "" instead of a number for unavailable prices.
public class PnjDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        JsonTokenType.Number => reader.GetDecimal(),
        JsonTokenType.String => decimal.TryParse(reader.GetString(), out var v) ? v : null,
        _ => null
    };

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteNumberValue(value.Value);
        else writer.WriteNullValue();
    }
}