namespace SourceBase.Application.Shared.Interfaces;

public interface IGoldPriceScraper
{
    GoldSource Source { get; }
    Task<(decimal BuyPrice, decimal SellPrice)?> ScrapeAsync(CancellationToken ct);
}
