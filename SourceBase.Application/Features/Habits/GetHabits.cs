using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Habits;

public record GetHabitsRequest;
public record HabitResponse(Guid Id, string Name, string? Icon, bool IsSystem);

public class GetHabitsEndpoint : IEndpoint
{
    public const string Route = "habits";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GetHabitsHandler handler, CancellationToken ct) => handler.Handle(new GetHabitsRequest(), ct))
        .WithTags("Habits");
}

public class GetHabitsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetHabitsRequest, List<HabitResponse>>
{
    public async Task<List<HabitResponse>> Handle(GetHabitsRequest request, CancellationToken ct) =>
        await dbContext.Habits
            .Where(h => h.IsSystem || h.UserId == currentUser.UserId)
            .OrderBy(h => h.Name)
            .Select(h => new HabitResponse(h.Id, h.Name, h.Icon, h.IsSystem))
            .ToListAsync(ct);
}
