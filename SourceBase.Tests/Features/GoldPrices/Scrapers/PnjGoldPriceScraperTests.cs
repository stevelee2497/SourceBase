using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class PnjGoldPriceScraperTests
{
    private const string ValidHtml = """
        <html><body>
        <table>
          <tr><th>Loại vàng</th><th>Mua vào</th><th>Bán ra</th></tr>
          <tr><td>Nhẫn Tròn 9999</td><td>1.900.000</td><td>1.950.000</td></tr>
          <tr><td>PNJ 1 Lượng</td><td>75.000.000</td><td>76.000.000</td></tr>
        </table>
        </body></html>
        """;

    private const string NoMatchHtml = """
        <html><body>
        <table>
          <tr><td>SJC 1 Lượng</td><td>75.000.000</td><td>76.000.000</td></tr>
        </table>
        </body></html>
        """;

    [Fact(DisplayName = "PNJ-SCRAPER-001: ScrapeAsync_WithValidHtml_ReturnsParsedPrices")]
    public async Task ScrapeAsync_WithValidHtml_ReturnsParsedPrices()
    {
        // Arrange
        var scraper = CreateScraperWithResponse(HttpStatusCode.OK, ValidHtml);

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(1_900_000m);
        result!.Value.SellPrice.Should().Be(1_950_000m);
    }

    [Fact(DisplayName = "PNJ-SCRAPER-002: ScrapeAsync_WithNoMatchingRow_ReturnsNull")]
    public async Task ScrapeAsync_WithNoMatchingRow_ReturnsNull()
    {
        // Arrange
        var scraper = CreateScraperWithResponse(HttpStatusCode.OK, NoMatchHtml);

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "PNJ-SCRAPER-003: ScrapeAsync_WithNoTable_ReturnsNull")]
    public async Task ScrapeAsync_WithNoTable_ReturnsNull()
    {
        // Arrange
        var scraper = CreateScraperWithResponse(HttpStatusCode.OK, "<html><body><p>No table here</p></body></html>");

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    private static PnjGoldPriceScraper CreateScraperWithResponse(HttpStatusCode status, string content)
    {
        var handler = new FakeHttpMessageHandler(status, content);
        var factory = new FakeHttpClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://www.pnj.com.vn") });
        return new PnjGoldPriceScraper(factory, NullLogger<PnjGoldPriceScraper>.Instance);
    }
}
