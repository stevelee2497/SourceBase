using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations.Scrapers;

public class NgocThinhGoldPriceScraper(ILogger<NgocThinhGoldPriceScraper> logger) : IGoldPriceScraper
{
    public GoldSource Source => GoldSource.NgocThinh;
    public string Url => "https://ngocthinh-jewelry.vn/pages/bang-gia-vang";

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string data, CancellationToken ct)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(data);

        var labelNode = doc.DocumentNode.SelectSingleNode(
            "//div[contains(@class,'headerindex1') and contains(.,'9999')]");

        if (labelNode is null)
        {
            logger.LogWarning("NgocThinh: could not find 'Vàng 9999' row");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        var parent = labelNode.ParentNode;
        var buyNode = parent.SelectSingleNode("./div[contains(@class,'headerindex2')]");
        var sellNode = parent.SelectSingleNode("./div[contains(@class,'headerindex3')]");

        if (buyNode is null || sellNode is null)
        {
            logger.LogWarning("NgocThinh: could not find buy/sell price nodes");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        if (!TryParseVnd(buyNode.InnerText, out var buyPrice) || !TryParseVnd(sellNode.InnerText, out var sellPrice))
        {
            logger.LogWarning("NgocThinh: failed to parse prices '{Buy}' / '{Sell}'", buyNode.InnerText, sellNode.InnerText);
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>((buyPrice, sellPrice));
    }

    private static bool TryParseVnd(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Replace(".", "").Replace(",", "").Trim();
        return decimal.TryParse(normalized, out value) && value > 0;
    }
}
