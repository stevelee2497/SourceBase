using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.GoldPrices;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices;

public class CreateGoldPriceTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "GOLDPRICE-CREATE-001: CreateGoldPrice_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateGoldPrice_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            source = "SJC",
            buyPrice = 1_900_000m,
            sellPrice = 1_950_000m,
            recordedAt = DateTime.UtcNow,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-002: CreateGoldPrice_WithValidSjcData_ReturnsOkAndId")]
    public async Task CreateGoldPrice_WithValidSjcData_ReturnsOkAndId()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            source = "SJC",
            buyPrice = 1_900_000m,
            sellPrice = 1_950_000m,
            recordedAt = DateTime.UtcNow,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateGoldPriceResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-003: CreateGoldPrice_WithMissingSource_ReturnsBadRequest")]
    public async Task CreateGoldPrice_WithMissingSource_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            buyPrice = 1_900_000m,
            sellPrice = 1_950_000m,
            recordedAt = DateTime.UtcNow,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-004: CreateGoldPrice_WithZeroBuyPrice_ReturnsBadRequest")]
    public async Task CreateGoldPrice_WithZeroBuyPrice_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            source = "SJC",
            buyPrice = 0m,
            sellPrice = 1_950_000m,
            recordedAt = DateTime.UtcNow,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-005: CreateGoldPrice_WithNegativeSellPrice_ReturnsBadRequest")]
    public async Task CreateGoldPrice_WithNegativeSellPrice_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            source = "SJC",
            buyPrice = 1_900_000m,
            sellPrice = -1m,
            recordedAt = DateTime.UtcNow,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-006: CreateGoldPrice_WithAllFourSources_ReturnsOk")]
    public async Task CreateGoldPrice_WithAllFourSources_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var sources = new[] { "SJC", "PNJ", "GiaVang", "KimKhanhVietHung" };

        foreach (var source in sources)
        {
            // Act
            var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
            {
                source,
                buyPrice = 1_900_000m,
                sellPrice = 1_950_000m,
                recordedAt = DateTime.UtcNow.AddHours(-Array.IndexOf(sources, source)),
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"source '{source}' should be valid");
        }
    }
}
