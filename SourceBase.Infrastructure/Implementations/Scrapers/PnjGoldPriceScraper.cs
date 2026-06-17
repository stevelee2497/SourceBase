using System.Text.Json;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations.Scrapers;

public class PnjGoldPriceScraper(ILogger<PnjGoldPriceScraper> logger) : IGoldPriceScraper
{
    public GoldSource Source => GoldSource.PNJ;

    public string Url => "https://edge-cf-api.pnj.io/ecom-frontend/v1/get-gold-price?zone=00";

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string jsonData, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(jsonData);
        if (!doc.RootElement.TryGetProperty("data", out var data))
        {
            logger.LogWarning("PNJ: missing 'data' array in response");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        foreach (var item in data.EnumerateArray())
        {
            var masp = item.TryGetProperty("masp", out var maspEl) ? maspEl.GetString() : null;
            if (masp != "N24K") continue;

            if (!item.TryGetProperty("giamua", out var giaMua) || giaMua.ValueKind != JsonValueKind.Number ||
                !item.TryGetProperty("giaban", out var giaBan) || giaBan.ValueKind != JsonValueKind.Number)
            {
                logger.LogWarning("PNJ: failed to read prices for masp '{Masp}'", masp);
                return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
            }

            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(
                (giaMua.GetDecimal() * 1000, giaBan.GetDecimal() * 1000));
        }

        logger.LogWarning("PNJ: could not find masp '{Masp}' in response", "N24K");
        return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
    }
}
