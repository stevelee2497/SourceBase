using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.HabitLogs;

public record CreateHabitLogRequest(string? HabitId, string? HabitName, HabitLogAction Action, DateTime OccurredAt);

public record CreateHabitLogResponse(Guid Id);

public class CreateHabitLogEndpoint : IEndpoint
{
    public const string Route = "habit-logs";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateHabitLogRequest request, CreateHabitLogHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("HabitLogs");
}

public class CreateHabitLogHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateHabitLogRequest, CreateHabitLogResponse>
{
    public async Task<CreateHabitLogResponse> Handle(CreateHabitLogRequest request, CancellationToken ct)
    {
        var entry = new HabitLogEntity
        {
            UserId = currentUser.UserId,
            HabitId = request.HabitId,
            HabitName = request.HabitName,
            Action = request.Action,
            OccurredAt = request.OccurredAt,
        };
        dbContext.HabitLogs.Add(entry);
        await dbContext.SaveChangesAsync(ct);
        return new CreateHabitLogResponse(entry.Id);
    }
}

public class CreateHabitLogRequestValidator : AbstractValidator<CreateHabitLogRequest>
{
    public CreateHabitLogRequestValidator()
    {
        RuleFor(x => x.OccurredAt).NotEmpty();
    }
}
