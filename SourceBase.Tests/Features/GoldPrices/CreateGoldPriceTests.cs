using System.Net;
using System.Net.Http.Json;
using Shouldly;
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
            items = new[]
            {
                new { source = "SJC", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = DateTime.UtcNow },
            },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-002: CreateGoldPrice_WithValidSjcData_ReturnsOkAndIds")]
    public async Task CreateGoldPrice_WithValidSjcData_ReturnsOkAndIds()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "SJC", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = DateTime.UtcNow },
            },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateGoldPriceResponse>();
        body!.Ids.Count.ShouldBe(1);
        body!.Ids[0].ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-003: CreateGoldPrice_WithMissingSource_ReturnsBadRequest")]
    public async Task CreateGoldPrice_WithMissingSource_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = DateTime.UtcNow },
            },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-004: CreateGoldPrice_WithZeroBuyPrice_ReturnsBadRequest")]
    public async Task CreateGoldPrice_WithZeroBuyPrice_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "SJC", buyPrice = 0m, sellPrice = 1_950_000m, recordedAt = DateTime.UtcNow },
            },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-005: CreateGoldPrice_WithNegativeSellPrice_ReturnsBadRequest")]
    public async Task CreateGoldPrice_WithNegativeSellPrice_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "SJC", buyPrice = 1_900_000m, sellPrice = -1m, recordedAt = DateTime.UtcNow },
            },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-006: CreateGoldPrice_WithAllFourSources_ReturnsOkWithFourIds")]
    public async Task CreateGoldPrice_WithAllFourSources_ReturnsOkWithFourIds()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var now = DateTime.UtcNow;

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "SJC", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = now },
                new { source = "PNJ", buyPrice = 1_880_000m, sellPrice = 1_930_000m, recordedAt = now.AddHours(-1) },
                new { source = "GiaVang", buyPrice = 1_860_000m, sellPrice = 1_910_000m, recordedAt = now.AddHours(-2) },
                new { source = "KimKhanhVietHung", buyPrice = 1_840_000m, sellPrice = 1_890_000m, recordedAt = now.AddHours(-3) },
            },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateGoldPriceResponse>();
        body!.Ids.Count.ShouldBe(4);
        body!.Ids.ShouldAllBe(id => id != Guid.Empty);
    }
}
