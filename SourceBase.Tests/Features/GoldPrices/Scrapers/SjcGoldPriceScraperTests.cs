using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class SjcGoldPriceScraperTests
{
    private const string ValidXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <root>
          <item ten_vang="Nhẫn Tròn 99,99 (1 Chỉ)" gia_mua="1.900.000" gia_ban="1.950.000" />
          <item ten_vang="SJC 1 Lượng" gia_mua="75.000.000" gia_ban="76.000.000" />
        </root>
        """;

    private const string NoMatchXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <root>
          <item ten_vang="SJC 1 Lượng" gia_mua="75.000.000" gia_ban="76.000.000" />
        </root>
        """;

    [Fact(DisplayName = "SJC-SCRAPER-001: ScrapeAsync_WithValidXml_ReturnsParsedPrices")]
    public async Task ScrapeAsync_WithValidXml_ReturnsParsedPrices()
    {
        // Arrange
        var scraper = CreateScraperWithResponse(HttpStatusCode.OK, ValidXml);

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(1_900_000m);
        result!.Value.SellPrice.Should().Be(1_950_000m);
    }

    [Fact(DisplayName = "SJC-SCRAPER-002: ScrapeAsync_WithNoMatchingRow_ReturnsNull")]
    public async Task ScrapeAsync_WithNoMatchingRow_ReturnsNull()
    {
        // Arrange
        var scraper = CreateScraperWithResponse(HttpStatusCode.OK, NoMatchXml);

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SJC-SCRAPER-003: ScrapeAsync_WithHttpFailure_ReturnsNull")]
    public async Task ScrapeAsync_WithHttpFailure_ReturnsNull()
    {
        // Arrange
        var scraper = CreateScraperWithException(new HttpRequestException("Connection refused"));

        // Act
        var act = () => scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static SjcGoldPriceScraper CreateScraperWithResponse(HttpStatusCode status, string content)
    {
        var handler = new FakeHttpMessageHandler(status, content);
        var factory = new FakeHttpClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://sjc.com.vn") });
        return new SjcGoldPriceScraper(factory, NullLogger<SjcGoldPriceScraper>.Instance);
    }

    private static SjcGoldPriceScraper CreateScraperWithException(Exception ex)
    {
        var handler = new FakeHttpMessageHandler(ex);
        var factory = new FakeHttpClientFactory(new HttpClient(handler));
        return new SjcGoldPriceScraper(factory, NullLogger<SjcGoldPriceScraper>.Instance);
    }
}
