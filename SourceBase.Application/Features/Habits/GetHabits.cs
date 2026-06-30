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
    public async Task<List<HabitResponse>> Handle(GetHabitsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var habits = await dbContext.Habits
            .Where(h => h.IsSystem || h.UserId == userId)
            .Select(h => new { h.Id, h.Name, h.Icon, h.IsSystem })
            .ToListAsync(ct);

        var habitIds = habits.Select(h => h.Id.ToString()).ToList();
        var counts = await dbContext.HabitLogs
            .Where(l => l.UserId == userId && habitIds.Contains(l.HabitId!))
            .GroupBy(l => l.HabitId)
            .Select(g => new { HabitId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.HabitId, x => x.Count, ct);

        return habits
            .Select(h => new HabitResponse(h.Id, h.Name, h.Icon, h.IsSystem, counts.GetValueOrDefault(h.Id.ToString(), 0)))
            .OrderByDescending(h => h.LogCount)
            .ToList();
    }
}
