using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.GoldPrices;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices;

[EndpointFact(
    Feature = "GoldPrices",
    Name = "Get Gold Prices",
    Route = "GET /api/gold-prices",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve a paginated and filterable list of gold price records, so that I can view price history and build charts. I can also request the latest record per source to display a summary dashboard.",
    Description = new[]
    {
        "Client sends optional query parameters: `source` (GoldSource), `dateFrom` (DateTime), `dateTo` (DateTime), `latest` (bool), `page` (default 1), `limit` (default 20), `order` (Asc / Desc, default Desc), `orderBy` (RecordedAt / BuyPrice / SellPrice / Source, default RecordedAt).",
        "If `latest=true`: returns one record per source (the most recent `RecordedAt` for each source). Pagination params are ignored; `page=1`, `limit=items.Count`, `total=items.Count`.",
        "Otherwise: results are filtered by the provided parameters and paginated.",
        "Returns `{ items, page, limit, total }`.",
    })]
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

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-007: GetGoldPrices_WithLatestTrue_ReturnsLatestRecordPerSource")]
    public async Task GetGoldPrices_WithLatestTrue_ReturnsLatestRecordPerSource()
    {
        // Arrange — far-future timestamp guarantees this is the latest SJC record across all tests
        var client = await factory.CreateAuthorizedClient($"goldprice_latest_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var t = new DateTime(2099, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "SJC", buyPrice = 2_000_000m, sellPrice = 2_100_000m, recordedAt = t } },
        });

        // Act
        var response = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?latest=true");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body.ShouldNotBeNull();
        body!.Items.ShouldContain(x => x.Source.ToString() == "SJC");
        var sjc = body.Items.First(x => x.Source.ToString() == "SJC");
        sjc.BuyPrice.ShouldBe(2_000_000m);
        sjc.SellPrice.ShouldBe(2_100_000m);
        sjc.RecordedAt.ShouldBe(t);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-008: GetGoldPrices_WithLatestTrue_MultipleRecordsPerSource_ReturnsOnlyLatest")]
    public async Task GetGoldPrices_WithLatestTrue_MultipleRecordsPerSource_ReturnsOnlyLatest()
    {
        // Arrange — far-future timestamps guarantee this PNJ record is the latest across all tests
        var client = await factory.CreateAuthorizedClient($"goldprice_latest_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var older = new DateTime(2098, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2098, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "PNJ", buyPrice = 1_000_000m, sellPrice = 1_100_000m, recordedAt = older } },
        });
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "PNJ", buyPrice = 1_500_000m, sellPrice = 1_600_000m, recordedAt = newer } },
        });

        // Act
        var response = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?latest=true");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        body!.Items.ShouldContain(x => x.Source.ToString() == "PNJ");
        var pnj = body.Items.First(x => x.Source.ToString() == "PNJ");
        pnj.BuyPrice.ShouldBe(1_500_000m);
        pnj.SellPrice.ShouldBe(1_600_000m);
        pnj.RecordedAt.ShouldBe(newer);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-ALL-009: GetGoldPrices_WithLatestTrue_AllFiveSourcesSeeded_ReturnsOneItemPerSource")]
    public async Task GetGoldPrices_WithLatestTrue_AllFiveSourcesSeeded_ReturnsOneItemPerSource()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"goldprice_latest_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var baseTime = new DateTime(2043, 5, 1, 6, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[]
            {
                new { source = "SJC",              buyPrice = 1_900_000m, sellPrice = 1_950_000m, recordedAt = baseTime },
                new { source = "PNJ",              buyPrice = 1_880_000m, sellPrice = 1_930_000m, recordedAt = baseTime },
                new { source = "GiaVang",          buyPrice = 1_860_000m, sellPrice = 1_910_000m, recordedAt = baseTime },
                new { source = "KimKhanhVietHung", buyPrice = 1_840_000m, sellPrice = 1_890_000m, recordedAt = baseTime },
                new { source = "NgocThinh",        buyPrice = 1_820_000m, sellPrice = 1_870_000m, recordedAt = baseTime },
            },
        });

        // Act
        var response = await client.GetAsync($"{GetGoldPricesEndpoint.Route}?latest=true");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GoldPriceResponse>>();
        var sources = body!.Items.Select(x => x.Source.ToString()).ToList();
        sources.ShouldContain("SJC");
        sources.ShouldContain("PNJ");
        sources.ShouldContain("GiaVang");
        sources.ShouldContain("KimKhanhVietHung");
        sources.ShouldContain("NgocThinh");
        body.Items.Select(x => x.Source.ToString()).Distinct().Count().ShouldBe(body.Items.Count);
    }
}
