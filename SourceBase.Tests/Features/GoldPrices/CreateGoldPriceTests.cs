using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.GoldPrices;
using SourceBase.Application.Shared;
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

    [Fact(DisplayName = "GOLDPRICE-CREATE-007: CreateGoldPrice_WithMinutesInRecordedAt_StoresFlooredToHour")]
    public async Task CreateGoldPrice_WithMinutesInRecordedAt_StoresFlooredToHour()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var recordedAt = new DateTime(2019, 5, 10, 8, 25, 30, DateTimeKind.Utc);
        var expectedHour = new DateTime(2019, 5, 10, 8, 0, 0, DateTimeKind.Utc);

        // Act
        var response = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "SJC", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt },
            },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dateFrom = Uri.EscapeDataString(expectedHour.ToString("o"));
        var dateTo = Uri.EscapeDataString(expectedHour.AddMinutes(59).ToString("o"));
        var getResponse = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?source=SJC&dateFrom={dateFrom}&dateTo={dateTo}");
        var body = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body!.Items.ShouldNotBeEmpty();
        body.Items.ShouldAllBe(x => x.RecordedAt.Minute == 0 && x.RecordedAt.Second == 0);
    }

    [Fact(DisplayName = "GOLDPRICE-CREATE-008: CreateGoldPrice_WithDuplicateSourceAndHour_UpdatesPricesAndReturnsSameId")]
    public async Task CreateGoldPrice_WithDuplicateSourceAndHour_UpdatesPricesAndReturnsSameId()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var baseHour = new DateTime(2019, 5, 10, 14, 0, 0, DateTimeKind.Utc);

        var firstResponse = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "PNJ", buyPrice = 1_800_000m, sellPrice = 1_850_000m, recordedAt = baseHour.AddMinutes(10) },
            },
        });
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<CreateGoldPriceResponse>();
        var firstId = firstBody!.Ids[0];

        // Act - same source, same hour, different minute → upsert
        var secondResponse = await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "PNJ", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = baseHour.AddMinutes(45) },
            },
        });

        // Assert
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<CreateGoldPriceResponse>();
        secondBody!.Ids[0].ShouldBe(firstId);

        var dateFrom = Uri.EscapeDataString(baseHour.ToString("o"));
        var dateTo = Uri.EscapeDataString(baseHour.AddMinutes(59).ToString("o"));
        var getResponse = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?source=PNJ&dateFrom={dateFrom}&dateTo={dateTo}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        getBody!.Items.Count.ShouldBe(1);
        getBody.Items[0].BuyPrice.ShouldBe(1_900_000m);
        getBody.Items[0].SellPrice.ShouldBe(1_950_000m);
    }
}
