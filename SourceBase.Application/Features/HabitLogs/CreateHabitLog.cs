using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.HabitLogs;

public record HabitLogEntry(Guid? HabitId, string? HabitName, HabitLogAction Action, DateTime OccurredAt);

public record CreateHabitLogsRequest(List<HabitLogEntry> Entries);

public record CreateHabitLogsResponse(List<Guid> Ids);

public class CreateHabitLogsEndpoint : IEndpoint
{
    public const string Route = "habit-logs";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateHabitLogsRequest request, CreateHabitLogsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("HabitLogs");
}

public class CreateHabitLogsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateHabitLogsRequest, CreateHabitLogsResponse>
{
    public async Task<CreateHabitLogsResponse> Handle(CreateHabitLogsRequest request, CancellationToken ct)
    {
        var entities = request.Entries
            .Select(e => new HabitLogEntity
            {
                UserId = currentUser.UserId,
                HabitId = e.HabitId,
                HabitName = e.HabitName,
                Action = e.Action,
                OccurredAt = e.OccurredAt,
            })
            .ToList();

        dbContext.HabitLogs.AddRange(entities);
        await dbContext.SaveChangesAsync(ct);
        return new CreateHabitLogsResponse(entities.Select(e => e.Id).ToList());
    }
}

public class CreateHabitLogsRequestValidator : AbstractValidator<CreateHabitLogsRequest>
{
    public CreateHabitLogsRequestValidator()
    {
        RuleFor(x => x.Entries).NotEmpty();
        RuleForEach(x => x.Entries).ChildRules(entry => entry.RuleFor(x => x.OccurredAt).NotEmpty());
    }
}
