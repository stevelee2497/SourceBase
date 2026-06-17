namespace SourceBase.Application.Shared.Interfaces;

public interface IGoldPriceScraper
{
    GoldSource Source { get; }
    string Url { get; }
    Task<(decimal BuyPrice, decimal SellPrice)?> ParseAsync(string data, CancellationToken ct);
}
