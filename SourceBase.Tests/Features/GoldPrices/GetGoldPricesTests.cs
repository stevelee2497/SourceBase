using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.GoldPrices;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices;

public class GetGoldPricesTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "GOLDPRICE-GET-ALL-001: GetGoldPrices_WithoutToken_ReturnsUnauthorized")]
    public async Task GetGoldPrices_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetGoldPricesEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-002: GetGoldPrices_WithNoFilter_ReturnsPaginatedList")]
    public async Task GetGoldPrices_WithNoFilter_ReturnsPaginatedList()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new
            {
                source = "SJC",
                buyPrice = 1_900_000m,
                sellPrice = 1_950_000m,
                recordedAt = DateTime.UtcNow,
            } }
        });

        // Act
        var response = await client.GetAsync(GetGoldPricesEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body!.Items.ShouldNotBeNull();
        body.Total.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-003: GetGoldPrices_FilterBySource_ReturnsOnlyMatchingSource")]
    public async Task GetGoldPrices_FilterBySource_ReturnsOnlyMatchingSource()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"goldprice_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var recordedAt = DateTime.UtcNow;
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new { items = new[] { new { source = "SJC", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt } } });
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new { items = new[] { new { source = "PNJ", buyPrice = 1_880_000m, sellPrice = 1_930_000m, recordedAt = recordedAt.AddSeconds(1) } } });

        // Act
        var response = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?source=SJC&limit=50");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body!.Items.ShouldNotBeEmpty();
        body.Items.ShouldAllBe(x => x.Source.ToString() == "SJC");
    }

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-004: GetGoldPrices_FilterByDateRange_ReturnsMatchingRange")]
    public async Task GetGoldPrices_FilterByDateRange_ReturnsMatchingRange()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"goldprice_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var recentDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new { items = new[] { new { source = "SJC", buyPrice = 1_800_000m, sellPrice = 1_850_000m, recordedAt = oldDate } } });
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new { items = new[] { new { source = "PNJ", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = recentDate } } });

        var dateFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("o");
        var dateTo = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("o");

        // Act
        var response = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}&limit=50");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body!.Items.ShouldNotContain(x => x.RecordedAt == oldDate);
        body.Items.ShouldContain(x => x.RecordedAt == recentDate);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-005: GetGoldPrices_WithPagination_ReturnsCorrectPage")]
    public async Task GetGoldPrices_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"goldprice_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var sources = new[] { "SJC", "PNJ", "GiaVang", "KimKhanhVietHung", "SJC" };
        var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
            {
                items = new[] { new
                {
                    source = sources[i],
                    buyPrice = 1_900_000m + i,
                    sellPrice = 1_950_000m + i,
                    recordedAt = baseTime.AddHours(i),
                } }
            });
        }

        // Act
        var response = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?page=2&limit=2&order=Asc&orderBy=BuyPrice&dateFrom={Uri.EscapeDataString(baseTime.ToString("o"))}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body!.Items.Count.ShouldBe(2);
        body.Total.ShouldBeGreaterThanOrEqualTo(5);
        body.Page.ShouldBe(2);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-006: GetGoldPrices_DefaultOrder_ReturnsNewestFirst")]
    public async Task GetGoldPrices_DefaultOrder_ReturnsNewestFirst()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"goldprice_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var earlier = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var later = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new { items = new[] { new { source = "SJC", buyPrice = 1_800_000m, sellPrice = 1_850_000m, recordedAt = earlier } } });
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new { items = new[] { new { source = "PNJ", buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = later } } });

        // Act
        var response = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?limit=50&orderBy=RecordedAt&order=Desc");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body!.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
        var laterIdx = body.Items.FindIndex(x => x.RecordedAt == later);
        var earlierIdx = body.Items.FindIndex(x => x.RecordedAt == earlier);
        laterIdx.ShouldBeLessThan(earlierIdx);
    }
}
