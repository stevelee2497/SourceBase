using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class KimKhanhVietHungGoldPriceScraperTests
{
    private static readonly KimKhanhVietHungGoldPriceScraper Scraper = new(NullLogger<KimKhanhVietHungGoldPriceScraper>.Instance);

    [Fact(DisplayName = "KIMKHANH-SCRAPER-001: ParseAsync_WithRealPageHtml_ReturnsVang999Prices")]
    public async Task ParseAsync_WithRealPageHtml_ReturnsVang999Prices()
    {
        // Arrange
        var html = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Features/GoldPrices/Scrapers/data/kkvh.html"));

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(14_250_000m);
        result!.Value.SellPrice.Should().Be(14_450_000m);
    }

    [Fact(DisplayName = "KIMKHANH-SCRAPER-002: ParseAsync_WithNoMatchingRow_ReturnsNull")]
    public async Task ParseAsync_WithNoMatchingRow_ReturnsNull()
    {
        // Arrange
        var html = """
            <html><body>
            <table>
              <tr><td>Vàng Nhẫn Khâu 98</td><td>13.910.000<sup>đ</sup></td><td>14.110.000<sup>đ</sup></td><td>0<sup>đ</sup></td></tr>
            </table>
            </body></html>
            """;

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "KIMKHANH-SCRAPER-003: ParseAsync_WithNoTable_ReturnsNull")]
    public async Task ParseAsync_WithNoTable_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><p>Trang đang cập nhật</p></body></html>";

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
