using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations.Scrapers;

public class GiaVangGoldPriceScraper(ILogger<GiaVangGoldPriceScraper> logger) : IGoldPriceScraper
{
    public GoldSource Source => GoldSource.GiaVang;
    public string Url => "https://giavang.org/the-gioi";

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string html, CancellationToken ct)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // TODO: confirm exact selector by inspecting https://giavang.org/the-gioi
        var priceNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'price-xau-vnd')]");

        if (priceNode is null || !TryParseVnd(priceNode.InnerText, out var price))
        {
            logger.LogWarning("GiaVang: could not parse VND price from page");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>((price, price));
    }

    private static bool TryParseVnd(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Replace(".", "").Replace(",", "").Trim();
        return decimal.TryParse(normalized, out value) && value > 0;
    }
}
