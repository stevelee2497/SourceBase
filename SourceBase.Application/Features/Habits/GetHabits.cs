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
        return dbContext.Habits
            .Where(h => h.IsSystem || h.UserId == userId)
            .Select(h => new { h, Count = h.HabitLogs.Count(l => l.UserId == userId) })
            .OrderByDescending(x => x.Count)
            .Select(x => new HabitResponse(x.h.Id, x.h.Name, x.h.Icon, x.h.IsSystem, x.Count))
            .ToListAsync(ct);
    }
}
