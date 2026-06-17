using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class GiaVangGoldPriceScraperTests
{
    private const string ValidGoldPageHtml = """
        <html><body>
        <span class="price-xau">2350.50</span>
        </body></html>
        """;

    private const string ValidExchangeRateJson = """
        {"rates":{"VND":25000}}
        """;

    private const string NoSpotPriceHtml = """
        <html><body><p>Không tìm thấy giá</p></body></html>
        """;

    [Fact(DisplayName = "GIAVANG-SCRAPER-001: ScrapeAsync_WithValidPriceAndRate_ReturnsConvertedPrices")]
    public async Task ScrapeAsync_WithValidPriceAndRate_ReturnsConvertedPrices()
    {
        // Arrange
        // 2350.50 USD/oz × 25000 VND/USD × (3.75/31.1035) ≈ 7,099,117
        var scraper = CreateScraperWithResponses(ValidGoldPageHtml, ValidExchangeRateJson);

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().BeGreaterThan(0);
        result!.Value.SellPrice.Should().Be(result.Value.BuyPrice);
    }

    [Fact(DisplayName = "GIAVANG-SCRAPER-002: ScrapeAsync_WithNoSpotPrice_ReturnsNull")]
    public async Task ScrapeAsync_WithNoSpotPrice_ReturnsNull()
    {
        // Arrange
        var scraper = CreateScraperWithResponses(NoSpotPriceHtml, ValidExchangeRateJson);

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GIAVANG-SCRAPER-003: ScrapeAsync_WithExchangeRateFailure_ReturnsNull")]
    public async Task ScrapeAsync_WithExchangeRateFailure_ReturnsNull()
    {
        // Arrange — exchange rate returns 500
        var scraper = CreateScraperWithResponses(ValidGoldPageHtml, null);

        // Act
        var result = await scraper.ScrapeAsync(CancellationToken.None);

        // Assert — exchange rate fetch failed so price can't be converted
        result.Should().BeNull();
    }

    private static GiaVangGoldPriceScraper CreateScraperWithResponses(string goldPageHtml, string? exchangeRateJson)
    {
        var handler = new MultiResponseHttpMessageHandler(new[]
        {
            (HttpStatusCode.OK, goldPageHtml),
            (exchangeRateJson is null ? HttpStatusCode.InternalServerError : HttpStatusCode.OK, exchangeRateJson ?? string.Empty),
        });
        var factory = new FakeHttpClientFactory(new HttpClient(handler));
        return new GiaVangGoldPriceScraper(factory, NullLogger<GiaVangGoldPriceScraper>.Instance);
    }
}

file class MultiResponseHttpMessageHandler(IEnumerable<(HttpStatusCode Status, string Content)> responses) : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Content)> _queue = new(responses);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!_queue.TryDequeue(out var entry))
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        return Task.FromResult(new HttpResponseMessage(entry.Status)
        {
            Content = new StringContent(entry.Content),
        });
    }
}
