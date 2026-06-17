using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations.Scrapers;

public class GiaVangGoldPriceScraper(ILogger<GiaVangGoldPriceScraper> logger) : IGoldPriceScraper
{
    public GoldSource Source => GoldSource.GiaVang;
    public string Url => "https://giavang.org/the-gioi";

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string data, CancellationToken ct)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(data);

        // <strong>1 cây vàng theo giá vàng thế giới quy đổi sang tiền Việt Nam Đồng có giá là 137.837.364 VNĐ</strong>
        var node = doc.DocumentNode.SelectSingleNode(
            "//strong[contains(.,'1 cây vàng') and contains(.,'VNĐ')]");

        if (node is null)
        {
            logger.LogWarning("GiaVang: could not find '1 cây vàng ... VNĐ' element");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        var text = node.InnerText;
        const string marker = "có giá là ";
        var idx = text.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
        {
            logger.LogWarning("GiaVang: could not find price marker in '{Text}'", text);
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        var priceRaw = text[(idx + marker.Length)..].Split(' ')[0]; // e.g. "137.837.364"
        if (!TryParseVnd(priceRaw, out var cayPrice))
        {
            logger.LogWarning("GiaVang: failed to parse price '{Raw}'", priceRaw);
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        // 1 cây = 10 chỉ → price per chỉ
        var pricePerChi = Math.Round(cayPrice / 10, 0, MidpointRounding.AwayFromZero);
        return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>((pricePerChi, pricePerChi));
    }

    private static bool TryParseVnd(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Replace(".", "").Replace(",", "").Trim();
        return decimal.TryParse(normalized, out value) && value > 0;
    }
}
