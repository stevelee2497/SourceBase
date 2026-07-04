using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SourceBase.Infrastructure.BackgroundServices.Scrapers;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class GiaVangGoldPriceScraperTests
{
    private static readonly GiaVangGoldPriceScraper Scraper = new(NullLogger<GiaVangGoldPriceScraper>.Instance);

    [Fact(DisplayName = "GIAVANG-SCRAPER-001: real page HTML returns price per chi")]
    public async Task ParseAsync_WithRealPageHtml_ReturnsPricePerChi()
    {
        // Arrange
        var html = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Features/GoldPrices/Scrapers/data/giavang.html"));

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert — 137.837.364 VNĐ / 10 chỉ per cây = 13.783.736
        result.ShouldNotBeNull();
        result!.Value.BuyPrice.ShouldBe(13_783_736m);
        result!.Value.SellPrice.ShouldBe(13_783_736m);
    }

    [Fact(DisplayName = "GIAVANG-SCRAPER-002: missing price element returns null")]
    public async Task ParseAsync_WithMissingPriceElement_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><p>Không tìm thấy giá</p></body></html>";

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
