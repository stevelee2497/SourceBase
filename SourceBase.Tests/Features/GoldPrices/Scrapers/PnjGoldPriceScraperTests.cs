using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class PnjGoldPriceScraperTests
{
    private static readonly PnjGoldPriceScraper Scraper = new(NullLogger<PnjGoldPriceScraper>.Instance);

    [Fact(DisplayName = "PNJ-SCRAPER-001: ParseAsync_WithRealApiResponse_ReturnsN24KPrices")]
    public async Task ParseAsync_WithRealApiResponse_ReturnsN24KPrices()
    {
        // Arrange
        var json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Features/GoldPrices/Scrapers/data/pnj.json"));

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert — giamua: 14880 × 1000, giaban: 15180 × 1000
        result.ShouldNotBeNull();
        result!.Value.BuyPrice.ShouldBe(14_880_000m);
        result!.Value.SellPrice.ShouldBe(15_180_000m);
    }

    [Fact(DisplayName = "PNJ-SCRAPER-002: ParseAsync_WithMissingN24K_ReturnsNull")]
    public async Task ParseAsync_WithMissingN24K_ReturnsNull()
    {
        // Arrange
        var json = """{"data":[{"masp":"SJC","tensp":"Vàng miếng SJC 999.9","giaban":15180,"giamua":14980}]}""";

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "PNJ-SCRAPER-003: ParseAsync_WithMissingDataArray_ReturnsNull")]
    public async Task ParseAsync_WithMissingDataArray_ReturnsNull()
    {
        // Arrange
        var json = """{"chinhanh":"hochiminh"}""";

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
