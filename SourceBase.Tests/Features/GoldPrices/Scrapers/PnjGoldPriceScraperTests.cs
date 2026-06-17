using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class PnjGoldPriceScraperTests
{
    private static readonly PnjGoldPriceScraper Scraper = new(null!, NullLogger<PnjGoldPriceScraper>.Instance);

    [Fact(DisplayName = "PNJ-SCRAPER-001: ParseAsync_WithValidHtml_ReturnsParsedPrices")]
    public async Task ParseAsync_WithValidHtml_ReturnsParsedPrices()
    {
        // Arrange
        var html = """
            <html><body>
            <table>
              <tr><th>Loại vàng</th><th>Mua vào</th><th>Bán ra</th></tr>
              <tr><td>Nhẫn Tròn 9999</td><td>1.900.000</td><td>1.950.000</td></tr>
              <tr><td>PNJ 1 Lượng</td><td>75.000.000</td><td>76.000.000</td></tr>
            </table>
            </body></html>
            """;

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(1_900_000m);
        result!.Value.SellPrice.Should().Be(1_950_000m);
    }

    [Fact(DisplayName = "PNJ-SCRAPER-002: ParseAsync_WithNoMatchingRow_ReturnsNull")]
    public async Task ParseAsync_WithNoMatchingRow_ReturnsNull()
    {
        // Arrange
        var html = """
            <html><body>
            <table>
              <tr><td>SJC 1 Lượng</td><td>75.000.000</td><td>76.000.000</td></tr>
            </table>
            </body></html>
            """;

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "PNJ-SCRAPER-003: ParseAsync_WithNoTable_ReturnsNull")]
    public async Task ParseAsync_WithNoTable_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><p>No table here</p></body></html>";

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
