using System.Text.Json.Serialization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.TimeSheets;

public record GetTimeSheetsRequest(DateOnly? From, DateOnly? To, int? Page, int? Limit, PagingOrder? Order, GetTimeSheetsOrder? OrderBy) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

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
        var items = await dbContext.TimeSheets
            .Where(x => x.UserId == currentUser.UserId
                && (request.From == null || x.Date >= request.From)
                && (request.To == null || x.Date <= request.To))
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
