using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations.Scrapers;

public class SjcGoldPriceScraper(IHttpClientFactory httpClientFactory, ILogger<SjcGoldPriceScraper> logger) : IGoldPriceScraper
{
    private const string Url = "https://sjc.com.vn/xml/tygiavang.xml";

    public GoldSource Source => GoldSource.SJC;

    public async Task<string> ScrapeAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoldScraper");
        return await client.GetStringAsync(Url, ct);
    }

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string html, CancellationToken ct)
    {
        var doc = XDocument.Parse(html);
        var item = doc.Descendants("item")
            .FirstOrDefault(e =>
            {
                var name = (string?)e.Attribute("ten_vang") ?? string.Empty;
                return name.Contains("Nhẫn", StringComparison.OrdinalIgnoreCase) &&
                       (name.Contains("99,99", StringComparison.OrdinalIgnoreCase) || name.Contains("9999", StringComparison.OrdinalIgnoreCase));
            });

        if (item is null)
        {
            logger.LogWarning("SJC: could not find nhẫn tròn 9999 row in XML");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        var buyRaw = (string?)item.Attribute("gia_mua");
        var sellRaw = (string?)item.Attribute("gia_ban");

        if (!TryParseVnd(buyRaw, out var buy) || !TryParseVnd(sellRaw, out var sell))
        {
            logger.LogWarning("SJC: failed to parse buy={Buy} sell={Sell}", buyRaw, sellRaw);
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>((buy, sell));
    }

    private static bool TryParseVnd(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Replace(".", "").Replace(",", "").Trim();
        return decimal.TryParse(normalized, out value) && value > 0;
    }
}
