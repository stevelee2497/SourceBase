using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.BackgroundServices.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class SjcGoldPriceScraperTests
{
    private static readonly SjcGoldPriceScraper Scraper = new(NullLogger<SjcGoldPriceScraper>.Instance);

    [Fact(DisplayName = "SJC-SCRAPER-001: real API response returns SJC prices")]
    public async Task ParseAsync_WithRealApiResponse_ReturnsSjcPrices()
    {
        // Arrange
        var json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Features/GoldPrices/Scrapers/data/pnj.json"));

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert — giamua: 14980 × 1000, giaban: 15180 × 1000
        result.ShouldNotBeNull();
        result!.Value.BuyPrice.ShouldBe(14_980_000m);
        result!.Value.SellPrice.ShouldBe(15_180_000m);
    }

    [Fact(DisplayName = "SJC-SCRAPER-002: missing SJC returns null")]
    public async Task ParseAsync_WithMissingSjc_ReturnsNull()
    {
        // Arrange
        var json = """{"data":[{"masp":"N24K","tensp":"Nhẫn tròn 24K PNJ","giaban":15180,"giamua":14880}]}""";

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "SJC-SCRAPER-003: missing data array returns null")]
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
