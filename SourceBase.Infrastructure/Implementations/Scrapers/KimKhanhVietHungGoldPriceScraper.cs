using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations.Scrapers;

public class KimKhanhVietHungGoldPriceScraper(IHttpClientFactory httpClientFactory, ILogger<KimKhanhVietHungGoldPriceScraper> logger) : IGoldPriceScraper
{
    private const string Url = "https://kimkhanhviethung.vn/tra-cuu-gia-vang.html";

    public GoldSource Source => GoldSource.KimKhanhVietHung;

    public async Task<(decimal BuyPrice, decimal SellPrice)?> ScrapeAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoldScraper");
        var html = await client.GetStringAsync(Url, ct);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode.SelectNodes("//table//tr");
        if (rows is null)
        {
            logger.LogWarning("KimKhanhVietHung: no table rows found — page may be JS-rendered");
            return null;
        }

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("td");
            if (cells is null || cells.Count < 3) continue;

            var name = cells[0].InnerText.Trim();
            var isRing = name.Contains("Nhẫn", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("nhan", StringComparison.OrdinalIgnoreCase);
            var isChi = name.Contains("1 Chỉ", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("1 chi", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("9999", StringComparison.OrdinalIgnoreCase);

            if (!isRing || !isChi) continue;

            var buyRaw = cells[1].InnerText.Trim();
            var sellRaw = cells[2].InnerText.Trim();

            if (!TryParseVnd(buyRaw, out var buy) || !TryParseVnd(sellRaw, out var sell))
            {
                logger.LogWarning("KimKhanhVietHung: failed to parse buy={Buy} sell={Sell}", buyRaw, sellRaw);
                return null;
            }

            return (buy, sell);
        }

        logger.LogWarning("KimKhanhVietHung: could not find nhẫn tròn 1 chỉ row");
        return null;
    }

    private static bool TryParseVnd(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Replace(".", "").Replace(",", "").Trim();
        return decimal.TryParse(normalized, out value) && value > 0;
    }
}
