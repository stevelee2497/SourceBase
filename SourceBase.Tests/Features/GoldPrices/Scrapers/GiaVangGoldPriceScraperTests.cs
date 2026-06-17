using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class GiaVangGoldPriceScraperTests
{
    private static readonly GiaVangGoldPriceScraper Scraper = new(NullLogger<GiaVangGoldPriceScraper>.Instance);

    [Fact(DisplayName = "GIAVANG-SCRAPER-001: ParseAsync_WithValidVndPrice_ReturnsParsedPrices")]
    public async Task ParseAsync_WithValidVndPrice_ReturnsParsedPrices()
    {
        // Arrange
        // TODO: update selector class to match the actual element on https://giavang.org/the-gioi
        var html = """
            <html><body>
            <span class="price-xau-vnd">7.099.117</span>
            </body></html>
            """;

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(7_099_117m);
        result!.Value.SellPrice.Should().Be(7_099_117m);
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
