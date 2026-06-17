using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class GiaVangGoldPriceScraperTests
{
    private static readonly GiaVangGoldPriceScraper Scraper = new(NullLogger<GiaVangGoldPriceScraper>.Instance);

    [Fact(DisplayName = "GIAVANG-SCRAPER-001: ParseAsync_WithRealPageHtml_ReturnsPricePerChi")]
    public async Task ParseAsync_WithRealPageHtml_ReturnsPricePerChi()
    {
        // Arrange
        var html = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Features/GoldPrices/Scrapers/data/giavang.html"));

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert — 137.837.364 VNĐ / 10 chỉ per cây = 13.783.736
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(13_783_736m);
        result!.Value.SellPrice.Should().Be(13_783_736m);
    }

    [Fact(DisplayName = "GIAVANG-SCRAPER-002: ParseAsync_WithMissingPriceElement_ReturnsNull")]
    public async Task ParseAsync_WithMissingPriceElement_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><p>Không tìm thấy giá</p></body></html>";

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
