using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SourceBase.Infrastructure.Implementations.Scrapers;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices.Scrapers;

public class SjcGoldPriceScraperTests
{
    private static readonly SjcGoldPriceScraper Scraper = new(NullLogger<SjcGoldPriceScraper>.Instance);

    [Fact(DisplayName = "SJC-SCRAPER-001: ParseAsync_WithValidXml_ReturnsParsedPrices")]
    public async Task ParseAsync_WithValidXml_ReturnsParsedPrices()
    {
        // Arrange
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
              <item ten_vang="Nhẫn Tròn 99,99 (1 Chỉ)" gia_mua="1.900.000" gia_ban="1.950.000" />
              <item ten_vang="SJC 1 Lượng" gia_mua="75.000.000" gia_ban="76.000.000" />
            </root>
            """;

        // Act
        var result = await Scraper.ParseAsync(xml, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.BuyPrice.Should().Be(1_900_000m);
        result!.Value.SellPrice.Should().Be(1_950_000m);
    }

    [Fact(DisplayName = "SJC-SCRAPER-002: ParseAsync_WithNoMatchingRow_ReturnsNull")]
    public async Task ParseAsync_WithNoMatchingRow_ReturnsNull()
    {
        // Arrange
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
              <item ten_vang="SJC 1 Lượng" gia_mua="75.000.000" gia_ban="76.000.000" />
            </root>
            """;

        // Act
        var result = await Scraper.ParseAsync(xml, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
