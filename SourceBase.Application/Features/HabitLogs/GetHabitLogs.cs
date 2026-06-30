using System.Text.Json.Serialization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.HabitLogs;

public record GetHabitLogsRequest(HabitLogAction? Action, HabitLogAction[]? Ignore, DateTime? From, DateTime? To, int? Page, int? Limit, PagingOrder? Order, GetHabitLogsOrderBy? OrderBy)
    : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public record GetHabitLogResponse(Guid Id, Guid? HabitId, string? HabitName, HabitLogAction Action, DateTime OccurredAt, DateTime? CreatedOn);

public class GetHabitLogsEndpoint : IEndpoint
{
    public const string Route = "habit-logs";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetHabitLogsRequest request, GetHabitLogsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("HabitLogs");
}

public class GetHabitLogsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetHabitLogsRequest, PagingResponse<GetHabitLogResponse>>
{
    public async Task<PagingResponse<GetHabitLogResponse>> Handle(GetHabitLogsRequest request, CancellationToken ct)
    {
        var logs = await dbContext.HabitLogs
            .Where(x => x.UserId == currentUser.UserId
                && (request.Action == null || x.Action == request.Action)
                && (request.Ignore == null || request.Ignore.Length == 0 || !request.Ignore.Contains(x.Action))
                && (request.From == null || x.OccurredAt >= request.From)
                && (request.To == null || x.OccurredAt <= request.To))
            .PaginateAsync(x => new GetHabitLogResponse(x.Id, x.HabitId, x.HabitName, x.Action, x.OccurredAt, x.CreatedOn), request, ct);
        return logs;
    }
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GetHabitLogsOrderBy { OccurredAt, Action, HabitName, CreatedOn }
