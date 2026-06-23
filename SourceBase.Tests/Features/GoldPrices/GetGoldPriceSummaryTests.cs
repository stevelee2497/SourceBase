using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SourceBase.Application.Features.GoldPrices;
using SourceBase.Domain.Entities;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.GoldPrices;

public class GetGoldPriceSummaryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "GOLDPRICE-GET-SUMMARY-001: GetGoldPriceSummary_WithoutToken_ReturnsUnauthorized")]
    public async Task GetGoldPriceSummary_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-SUMMARY-002: GetGoldPriceSummary_WithAuthorizedToken_ReturnsOk")]
    public async Task GetGoldPriceSummary_WithAuthorizedToken_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
    }

    [Fact(DisplayName = "GOLDPRICE-GET-SUMMARY-003: GetGoldPriceSummary_AfterSeedingSingleSource_ReturnsCorrectPrices")]
    public async Task GetGoldPriceSummary_AfterSeedingSingleSource_ReturnsCorrectPrices()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var recordedAt = new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "NgocThinh", buyPrice = 1_111_000m, sellPrice = 1_222_000m, recordedAt } },
        });

        // Act
        var response = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        body!.Items.ShouldContain(x => x.Source.ToString() == "NgocThinh");
        var item = body.Items.First(x => x.Source.ToString() == "NgocThinh");
        item.BuyPrice.ShouldBe(1_111_000m);
        item.SellPrice.ShouldBe(1_222_000m);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-SUMMARY-004: GetGoldPriceSummary_WithAllFiveSourcesSeeded_ReturnsOneItemPerSource")]
    public async Task GetGoldPriceSummary_WithAllFiveSourcesSeeded_ReturnsOneItemPerSource()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var baseTime = new DateTime(2031, 3, 1, 6, 0, 0, DateTimeKind.Utc);
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
        var response = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        var sources = body!.Items.Select(x => x.Source.ToString()).ToList();
        sources.ShouldContain("SJC");
        sources.ShouldContain("PNJ");
        sources.ShouldContain("GiaVang");
        sources.ShouldContain("KimKhanhVietHung");
        sources.ShouldContain("NgocThinh");
        body.Items.Select(x => x.Source.ToString()).Distinct().Count().ShouldBe(body.Items.Count);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-SUMMARY-005: GetGoldPriceSummary_WithMultipleRecordsForSameSource_ReturnsLatestOnly")]
    public async Task GetGoldPriceSummary_WithMultipleRecordsForSameSource_ReturnsLatestOnly()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var older = new DateTime(2032, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2032, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "SJC", buyPrice = 1_000_000m, sellPrice = 1_100_000m, recordedAt = older } },
        });
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "SJC", buyPrice = 2_000_000m, sellPrice = 2_200_000m, recordedAt = newer } },
        });

        // Act
        var response = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        body!.Items.ShouldContain(x => x.Source.ToString() == "SJC");
        var sjc = body.Items.First(x => x.Source.ToString() == "SJC");
        sjc.BuyPrice.ShouldBe(2_000_000m);
        sjc.SellPrice.ShouldBe(2_200_000m);
        sjc.RecordedAt.ShouldBe(newer);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-SUMMARY-006: GetGoldPriceSummary_AfterPriceUpdate_ReflectsNewPrices")]
    public async Task GetGoldPriceSummary_AfterPriceUpdate_ReflectsNewPrices()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var hour = new DateTime(2033, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "PNJ", buyPrice = 1_500_000m, sellPrice = 1_600_000m, recordedAt = hour } },
        });

        // Act — create a newer record for the same source
        var newerHour = new DateTime(2033, 7, 1, 11, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "PNJ", buyPrice = 1_700_000m, sellPrice = 1_800_000m, recordedAt = newerHour } },
        });

        var response = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);

        // Assert — summary returns the updated (newer) prices
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        body!.Items.ShouldContain(x => x.Source.ToString() == "PNJ");
        var pnj = body.Items.First(x => x.Source.ToString() == "PNJ");
        pnj.BuyPrice.ShouldBe(1_700_000m);
        pnj.SellPrice.ShouldBe(1_800_000m);
    }

    [RequiresRedisFact(DisplayName = "GOLDPRICE-GET-SUMMARY-007: GetGoldPriceSummary_CachesResult_ServesStaleDataBeforeCacheIsInvalidated")]
    public async Task GetGoldPriceSummary_CachesResult_ServesStaleDataBeforeCacheIsInvalidated()
    {
        // Arrange — use a far-future timestamp to be the definitive latest for KimKhanhVietHung
        var client = await factory.CreateAuthorizedClient();
        var recordedAt = new DateTime(2097, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Creating a gold price invokes RemoveAsync → cache is cold after this
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "KimKhanhVietHung", buyPrice = 500_000m, sellPrice = 550_000m, recordedAt } },
        });

        // Warm the cache — GET populates gold-price-summary with KimKhanhVietHung at 500_000m
        var firstResponse = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        firstBody!.Items.ShouldContain(x => x.Source == GoldSource.KimKhanhVietHung);
        firstBody.Items.First(x => x.Source == GoldSource.KimKhanhVietHung).BuyPrice.ShouldBe(500_000m);

        // Bypass the API and change BuyPrice directly in DB (no cache invalidation triggered)
        await factory.WithDbContextAsync(async db =>
        {
            var entity = await db.GoldPrices.FirstOrDefaultAsync(x => x.Source == GoldSource.KimKhanhVietHung && x.RecordedAt == recordedAt);
            entity!.BuyPrice = 999_999m;
            await db.SaveChangesAsync();
            return true;
        });

        // Act — second GET should still return the cached (stale) value
        var secondResponse = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);

        // Assert — Redis served the cached 500_000m; the direct DB change is invisible
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        secondBody!.Items.First(x => x.Source == GoldSource.KimKhanhVietHung).BuyPrice.ShouldBe(500_000m);
        secondBody.Items.First(x => x.Source == GoldSource.KimKhanhVietHung).BuyPrice.ShouldNotBe(999_999m);
    }

    [Fact(DisplayName = "GOLDPRICE-GET-SUMMARY-008: GetGoldPriceSummary_AfterCreateGoldPrice_CacheIsInvalidatedAndReturnsFreshPrices")]
    public async Task GetGoldPriceSummary_AfterCreateGoldPrice_CacheIsInvalidatedAndReturnsFreshPrices()
    {
        // Arrange — use a far-future base timestamp to be the definitive latest for GiaVang
        var client = await factory.CreateAuthorizedClient();
        var t1 = new DateTime(2098, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        // CreateGoldPrice invokes RemoveAsync so the cache is cold; then GET warms it
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "GiaVang", buyPrice = 1_100_000m, sellPrice = 1_200_000m, recordedAt = t1 } },
        });

        var cachedResponse = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cachedBody = await cachedResponse.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        cachedBody!.Items.First(x => x.Source == GoldSource.GiaVang).BuyPrice.ShouldBe(1_100_000m);

        // Act — create a newer GiaVang price; this should invalidate gold-price-summary
        var t2 = new DateTime(2098, 1, 1, 11, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync(CreateGoldPriceEndpoint.Route, new
        {
            items = new[] { new { source = "GiaVang", buyPrice = 1_300_000m, sellPrice = 1_400_000m, recordedAt = t2 } },
        });

        // Assert — GET re-fetches from DB and returns the updated (newer) price
        var freshResponse = await client.GetAsync(GetGoldPriceSummaryEndpoint.Route);
        freshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var freshBody = await freshResponse.Content.ReadFromJsonAsync<GetGoldPriceSummaryResponse>();
        freshBody!.Items.First(x => x.Source == GoldSource.GiaVang).BuyPrice.ShouldBe(1_300_000m);
    }
}
