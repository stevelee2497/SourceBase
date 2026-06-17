namespace SourceBase.Application.Shared.Interfaces;

public interface IGoldPriceScraper
{
    GoldSource Source { get; }
    Task<string> ScrapeAsync(CancellationToken ct);
    Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string html, CancellationToken ct);
}
