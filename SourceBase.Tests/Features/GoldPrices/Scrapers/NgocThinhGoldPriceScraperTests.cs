using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.BackgroundServices.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class NgocThinhGoldPriceScraperTests
{
    private static readonly NgocThinhGoldPriceScraper Scraper = new(NullLogger<NgocThinhGoldPriceScraper>.Instance);

    [Fact(DisplayName = "NGOCTHINH-SCRAPER-001: real page html returns 99.99 prices")]
    public async Task ParseAsync_WithRealPageHtml_Returns9999Prices()
    {
        // Arrange
        var html = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Features/GoldPrices/Scrapers/data/ngocthinh.html"));

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert — headerindex2: 14.030.000, headerindex3: 14.160.000
        result.ShouldNotBeNull();
        result!.Value.BuyPrice.ShouldBe(14_030_000m);
        result!.Value.SellPrice.ShouldBe(14_160_000m);
    }

    [Fact(DisplayName = "NGOCTHINH-SCRAPER-002: missing 99.99 row returns null")]
    public async Task ParseAsync_WithMissing9999Row_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><div><div class=\"stylecus headerindex1\">Vàng 98</div><div class=\"stylecus headerindex2\">13.730.000</div><div class=\"stylecus headerindex3\">13.880.000</div></div></body></html>";

        // Act
        var result = await Scraper.ParseAsync(html, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "NGOCTHINH-SCRAPER-003: empty html returns null")]
    public async Task ParseAsync_WithEmptyHtml_ReturnsNull()
    {
        // Act
        var result = await Scraper.ParseAsync("<html><body></body></html>", CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
