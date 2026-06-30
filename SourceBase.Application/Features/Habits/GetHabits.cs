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
        var logCounts = dbContext.HabitLogs
            .Where(l => l.UserId == userId)
            .GroupBy(l => l.HabitId)
            .Select(g => new { HabitId = g.Key, Count = g.Count() });

        return dbContext.Habits
            .Where(h => h.IsSystem || h.UserId == userId)
            .GroupJoin(logCounts, h => h.Id.ToString(), lc => lc.HabitId, (h, lcs) => new { h, lcs })
            .SelectMany(x => x.lcs.DefaultIfEmpty(), (x, lc) => new HabitResponse(x.h.Id, x.h.Name, x.h.Icon, x.h.IsSystem, lc == null ? 0 : lc.Count))
            .OrderByDescending(r => r.LogCount)
            .ToListAsync(ct);
    }
}
