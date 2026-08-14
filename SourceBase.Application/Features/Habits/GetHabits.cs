using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Habits;

public record GetHabitsRequest(DateTime? From, DateTime? To);
public record HabitResponse(Guid Id, string Name, string? Icon, bool IsSystem, int LogCount);

public class GetHabitsEndpoint : IEndpoint
{
    public const string Route = "habits";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetHabitsRequest request, GetHabitsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Habits");
}

public class GetHabitsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetHabitsRequest, List<HabitResponse>>
{
    public Task<List<HabitResponse>> Handle(GetHabitsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return dbContext.Habits
            .Where(h => h.IsSystem || h.UserId == userId)
            .Select(h => new { h, Count = h.HabitLogs.Count(l => l.UserId == userId && (request.From == null || l.OccurredAt >= request.From) && (request.To == null || l.OccurredAt <= request.To)) })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.h.Name)
            .Select(x => new HabitResponse(x.h.Id, x.h.Name, x.h.Icon, x.h.IsSystem, x.Count))
            .ToListAsync(ct);
    }
}
