using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Habits;

public record DeleteHabitRequest(Guid Id);
public record DeleteHabitResponse(bool Success);

public class DeleteHabitEndpoint : IEndpoint
{
    public const string Route = "habits/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteHabitHandler handler, CancellationToken ct) => handler.Handle(new DeleteHabitRequest(id), ct))
        .WithTags("Habits");
}

public class DeleteHabitHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteHabitRequest, DeleteHabitResponse>
{
    public async Task<DeleteHabitResponse> Handle(DeleteHabitRequest request, CancellationToken ct)
    {
        var habit = await dbContext.Habits.FindAsync([request.Id], ct);
        if (habit is null) throw new NotFoundException();
        if (habit.IsSystem) throw new ForbiddenException();
        if (habit.UserId != currentUser.UserId) throw new NotFoundException();

        dbContext.Habits.Remove(habit);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteHabitResponse(true);
    }
}
