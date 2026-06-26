using System.Text.Json.Serialization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.TimeSheets;

public record GetTimeSheetsRequest(DateOnly? From, DateOnly? To, int? Year, int? Month, DateOnly? Date, int? Page, int? Limit, PagingOrder? Order, GetTimeSheetsOrder? OrderBy) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public class GetTimeSheetsEndpoint : IEndpoint
{
    public const string Route = "time-sheets";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTimeSheetsRequest request, GetTimeSheetsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TimeSheets");
}

public class GetTimeSheetsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTimeSheetsRequest, PagingResponse<GetTimeSheetResponse>>
{
    public async Task<PagingResponse<GetTimeSheetResponse>> Handle(GetTimeSheetsRequest request, CancellationToken ct)
    {
        var from = request.From ?? (request.Date ?? (request.Year != null && request.Month != null ? new DateOnly(request.Year.Value, request.Month.Value, 1) : (DateOnly?)null));
        var to = request.To ?? (request.Date ?? (request.Year != null && request.Month != null ? new DateOnly(request.Year.Value, request.Month.Value, 1).AddMonths(1).AddDays(-1) : (DateOnly?)null));

        var items = await dbContext.TimeSheets
            .Where(x => x.UserId == currentUser.UserId
                && (from == null || x.Date >= from)
                && (to == null || x.Date <= to))
            .PaginateAsync(x => new GetTimeSheetResponse(x), request, ct);
        return items;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GetTimeSheetsOrder
{
    Date,
    Project,
    Hours,
    CreatedOn,
    UpdatedOn,
}
