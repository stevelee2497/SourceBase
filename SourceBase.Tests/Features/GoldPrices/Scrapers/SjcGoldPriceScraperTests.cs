using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class SjcGoldPriceScraperTests
{
    private static readonly SjcGoldPriceScraper Scraper = new(NullLogger<SjcGoldPriceScraper>.Instance);

    [Fact(DisplayName = "SJC-SCRAPER-001: ParseAsync_WithRealApiResponse_ReturnsSjcPrices")]
    public async Task ParseAsync_WithRealApiResponse_ReturnsSjcPrices()
    {
        // Arrange
        var json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Features/GoldPrices/Scrapers/data/pnj.json"));

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert — giamua: 14980 × 1000, giaban: 15180 × 1000
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(14_980_000m);
        result!.Value.SellPrice.Should().Be(15_180_000m);
    }

    [Fact(DisplayName = "SJC-SCRAPER-002: ParseAsync_WithMissingSjc_ReturnsNull")]
    public async Task ParseAsync_WithMissingSjc_ReturnsNull()
    {
        // Arrange
        var json = """{"data":[{"masp":"N24K","tensp":"Nhẫn tròn 24K PNJ","giaban":15180,"giamua":14880}]}""";

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SJC-SCRAPER-003: ParseAsync_WithMissingDataArray_ReturnsNull")]
    public async Task ParseAsync_WithMissingDataArray_ReturnsNull()
    {
        // Arrange
        var json = """{"chinhanh":"hochiminh"}""";

        // Act
        var result = await Scraper.ParseAsync(json, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
