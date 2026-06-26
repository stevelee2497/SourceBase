using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.GoldPrices;

public record GetGoldPricesRequest(GoldSource? Source, DateTime? DateFrom, DateTime? DateTo, bool? Latest = null, int? Page = 1, int? Limit = 20, PagingOrder? Order = PagingOrder.Desc, GoldPriceOrderBy? OrderBy = GoldPriceOrderBy.RecordedAt) : PagingRequest(Page, Limit, Order, (OrderBy ?? GoldPriceOrderBy.RecordedAt).ToString());

public record GoldPriceResponse(Guid Id, GoldSource Source, decimal BuyPrice, decimal SellPrice, DateTime RecordedAt);

public class GetGoldPricesEndpoint : IEndpoint
{
    public const string Route = "gold-prices";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetGoldPricesRequest request, GetGoldPricesHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("GoldPrices");
}

public class GetGoldPricesHandler(IDbContext dbContext) : IRequestHandler<GetGoldPricesRequest, PagingResponse<GoldPriceResponse>>
{
    public async Task<PagingResponse<GoldPriceResponse>> Handle(GetGoldPricesRequest request, CancellationToken ct)
    {
        if (request.Latest == true)
            return await GetLatestPerSourceAsync(request, ct);

        return await dbContext.GoldPrices
            .Where(x =>
                (request.Source == null || x.Source == request.Source) &&
                (request.DateFrom == null || x.RecordedAt >= request.DateFrom) &&
                (request.DateTo == null || x.RecordedAt <= request.DateTo))
            .PaginateAsync(x => new GoldPriceResponse(x.Id, x.Source, x.BuyPrice, x.SellPrice, x.RecordedAt), request, ct);
    }

    private async Task<PagingResponse<GoldPriceResponse>> GetLatestPerSourceAsync(GetGoldPricesRequest request, CancellationToken ct)
    {
        var items = await (
            from p in dbContext.GoldPrices
            where (request.Source == null || p.Source == request.Source) &&
                  p.RecordedAt == dbContext.GoldPrices.Where(q => q.Source == p.Source).Max(q => q.RecordedAt)
            select new GoldPriceResponse(p.Id, p.Source, p.BuyPrice, p.SellPrice, p.RecordedAt)
        ).ToListAsync(ct);

        return new PagingResponse<GoldPriceResponse>(items, 1, items.Count, items.Count);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GoldPriceOrderBy
{
    RecordedAt,
    BuyPrice,
    SellPrice,
    Source
}
