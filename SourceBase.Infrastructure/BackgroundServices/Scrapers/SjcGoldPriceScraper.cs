using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.BackgroundServices.Scrapers;

public class SjcGoldPriceScraper(ILogger<SjcGoldPriceScraper> logger) : IGoldPriceScraper
{
    public GoldSource Source => GoldSource.SJC;

    public string Url => "https://edge-cf-api.pnj.io/ecom-frontend/v1/get-gold-price?zone=00";

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string jsonData, CancellationToken ct)
    {
        var response = jsonData.Deserialize<PnjGoldPriceResponse>();
        if (response is null || response.Data is null || response.Data.Count == 0)
        {
            logger.LogWarning("PNJ: empty or invalid response");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        var item = response.Data.FirstOrDefault(i => i.Masp.Equals("SJC", StringComparison.OrdinalIgnoreCase));
        return item is null || item.Giamua is null || item.Giaban is null
            ? Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null)
            : Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>((item.Giamua.Value * 1000, item.Giaban.Value * 1000));
    }
}
