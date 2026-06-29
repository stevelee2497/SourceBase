using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.HabitLogs;

public record DeleteHabitLogRequest(Guid Id);

public record DeleteHabitLogResponse(bool Success);

public class DeleteHabitLogEndpoint : IEndpoint
{
    public const string Route = "habit-logs/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteHabitLogHandler handler, CancellationToken ct) => handler.Handle(new DeleteHabitLogRequest(id), ct))
        .WithTags("HabitLogs");
}

public class DeleteHabitLogHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteHabitLogRequest, DeleteHabitLogResponse>
{
    public async Task<DeleteHabitLogResponse> Handle(DeleteHabitLogRequest request, CancellationToken ct)
    {
        var entry = await dbContext.HabitLogs.FindAsync([request.Id], ct);
        if (entry == null || entry.UserId != currentUser.UserId)
            throw new NotFoundException();

        dbContext.HabitLogs.Remove(entry);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteHabitLogResponse(true);
    }
}
