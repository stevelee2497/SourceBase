using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations.Scrapers;

public class GiaVangGoldPriceScraper(IHttpClientFactory httpClientFactory, ILogger<GiaVangGoldPriceScraper> logger) : IGoldPriceScraper
{
    private const string Url = "https://giavang.org/the-gioi";
    private const string ExchangeRateUrl = "https://open.er-api.com/v6/latest/USD";
    // 1 troy ounce = 31.1035 grams; 1 chỉ = 3.75 grams
    private const decimal ChiPerOunce = 3.75m / 31.1035m;

    public GoldSource Source => GoldSource.GiaVang;

    public async Task<(decimal BuyPrice, decimal SellPrice)?> ScrapeAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoldScraper");
        var html = await client.GetStringAsync(Url, ct);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find USD spot price — giavang.org/the-gioi shows a table with gold prices
        var spotNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'price-xau')]") ??
                       doc.DocumentNode.SelectSingleNode("//*[contains(@class,'gold-price') and contains(@class,'usd')]");

        decimal spotUsd;
        if (spotNode is null || !TryParseDecimal(spotNode.InnerText, out spotUsd))
        {
            logger.LogWarning("GiaVang: could not parse USD spot price from page");
            return null;
        }

        var usdVnd = await FetchUsdVndRateAsync(client, ct);
        if (usdVnd <= 0)
        {
            logger.LogWarning("GiaVang: could not fetch USD/VND exchange rate");
            return null;
        }

        var pricePerChi = spotUsd * usdVnd * ChiPerOunce;
        return (Math.Round(pricePerChi, 0), Math.Round(pricePerChi, 0));
    }

    private async Task<decimal> FetchUsdVndRateAsync(HttpClient client, CancellationToken ct)
    {
        try
        {
            var json = await client.GetStringAsync(ExchangeRateUrl, ct);
            // Response: { "rates": { "VND": 25000 } }
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty("VND", out var vnd))
                return vnd.GetDecimal();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GiaVang: exchange rate fetch failed");
        }
        return 0;
    }

    private static bool TryParseDecimal(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Replace(",", "").Trim();
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0;
    }
}
