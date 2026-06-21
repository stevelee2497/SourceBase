using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.GoldPrices;

public record GetGoldPriceSummaryRequest;

public record GetGoldPriceSummaryResponse(IReadOnlyList<GoldPriceResponse> Items);

public class GetGoldPriceSummaryEndpoint : IEndpoint
{
    public const string Route = "gold-prices/summary";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetGoldPriceSummaryRequest request, GetGoldPriceSummaryHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("GoldPrices");
}

public class GetGoldPriceSummaryHandler(IDbContext dbContext, ICacheService cacheService) : IRequestHandler<GetGoldPriceSummaryRequest, GetGoldPriceSummaryResponse>
{
    public static string CacheKey => "gold-price-summary";

    public async Task<GetGoldPriceSummaryResponse> Handle(GetGoldPriceSummaryRequest request, CancellationToken ct)
    {
        var cached = await cacheService.GetAsync<GetGoldPriceSummaryResponse>(CacheKey, ct);
        if (cached is not null) return cached;

        var items = await (
            from p in dbContext.GoldPrices
            where p.RecordedAt == dbContext.GoldPrices
                .Where(q => q.Source == p.Source)
                .Max(q => q.RecordedAt)
            select new GoldPriceResponse(p.Id, p.Source, p.BuyPrice, p.SellPrice, p.RecordedAt)
        ).ToListAsync(ct);

        var response = new GetGoldPriceSummaryResponse(items);
        await cacheService.SetAsync(CacheKey, response, TimeSpan.FromMinutes(30), ct);
        return response;
    }
}
