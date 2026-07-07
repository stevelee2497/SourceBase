using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.BackgroundServices.Scrapers;

public class BtcPriceScraper(ILogger<BtcPriceScraper> logger) : IGoldPriceScraper
{
    public GoldSource Source => GoldSource.BTC;

    public string Url => "https://api.binance.com/api/v3/ticker/price?symbol=BTCUSDT";

    public Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string jsonData, CancellationToken ct)
    {
        var response = jsonData.Deserialize<BinanceTickerPriceResponse>();
        if (response is null || !decimal.TryParse(response.Price, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var price))
        {
            logger.LogWarning("BTC: could not parse Binance ticker price");
            return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>(null);
        }

        return Task.FromResult<(decimal BuyPrice, decimal SellPrice)?>((price, price));
    }
}

public record BinanceTickerPriceResponse(string Symbol, string Price);
