using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Habits;

public record GetHabitsRequest;
public record HabitResponse(Guid Id, string Name, string? Icon, bool IsSystem, int LogCount);

public class GetHabitsEndpoint : IEndpoint
{
    public const string Route = "habits";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GetHabitsHandler handler, CancellationToken ct) => handler.Handle(new GetHabitsRequest(), ct))
        .WithTags("Habits");
}

public class GetHabitsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetHabitsRequest, List<HabitResponse>>
{
    public Task<List<HabitResponse>> Handle(GetHabitsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var habitCounts = dbContext.HabitLogs
            .Where(l => l.UserId == userId)
            .GroupBy(l => l.HabitId)
            .Select(g => new { HabitId = g.Key, Count = g.Count() });

        return dbContext.Habits
            .Where(h => h.IsSystem || h.UserId == userId)
            .GroupJoin(habitCounts, h => h.Id.ToString(), hc => hc.HabitId, (h, hcs) => new { h, hcs })
            .SelectMany(x => x.hcs.DefaultIfEmpty(), (x, hc) => new HabitResponse(x.h.Id, x.h.Name, x.h.Icon, x.h.IsSystem, hc == null ? 0 : hc.Count))
            .OrderByDescending(r => r.LogCount)
            .ToListAsync(ct);
    }
}
