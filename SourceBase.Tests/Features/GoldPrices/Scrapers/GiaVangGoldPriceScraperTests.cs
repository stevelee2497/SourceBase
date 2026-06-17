using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class GiaVangGoldPriceScraperTests
{
    [Fact(DisplayName = "GIAVANG-SCRAPER-001: ParseAsync_WithValidPriceAndRate_ReturnsConvertedPrices")]
    public async Task ParseAsync_WithValidPriceAndRate_ReturnsConvertedPrices()
    {
        // Arrange
        // 2350.50 USD/oz × 25000 VND/USD × (3.75/31.1035) ≈ 7,099,117
        var html = """
            <html><body>
            <span class="price-xau">2350.50</span>
            </body></html>
            """;
        var scraper = CreateScraper("""{"rates":{"VND":25000}}""");

        // Act
        var result = await scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().BeGreaterThan(0);
        result!.Value.SellPrice.Should().Be(result.Value.BuyPrice);
    }

    [Fact(DisplayName = "GIAVANG-SCRAPER-002: ParseAsync_WithNoSpotPrice_ReturnsNull")]
    public async Task ParseAsync_WithNoSpotPrice_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><p>Không tìm thấy giá</p></body></html>";
        var scraper = CreateScraper("""{"rates":{"VND":25000}}""");

        // Act
        var result = await scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GIAVANG-SCRAPER-003: ParseAsync_WithExchangeRateFailure_ReturnsNull")]
    public async Task ParseAsync_WithExchangeRateFailure_ReturnsNull()
    {
        // Arrange
        var html = """
            <html><body>
            <span class="price-xau">2350.50</span>
            </body></html>
            """;
        var scraper = CreateScraper(null);

        // Act
        var result = await scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    private static GiaVangGoldPriceScraper CreateScraper(string? exchangeRateJson)
    {
        var handler = new FakeHttpMessageHandler(
            exchangeRateJson is null ? HttpStatusCode.InternalServerError : HttpStatusCode.OK,
            exchangeRateJson ?? string.Empty);
        var factory = new FakeHttpClientFactory(new HttpClient(handler));
        return new GiaVangGoldPriceScraper(factory, NullLogger<GiaVangGoldPriceScraper>.Instance);
    }
}
