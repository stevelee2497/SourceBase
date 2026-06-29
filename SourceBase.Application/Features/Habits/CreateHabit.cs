using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Habits;

public record CreateHabitRequest(string Name, string? Icon);
public record CreateHabitResponse(Guid Id);

public class CreateHabitEndpoint : IEndpoint
{
    public const string Route = "habits";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateHabitRequest request, CreateHabitHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Habits");
}

public class CreateHabitHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateHabitRequest, CreateHabitResponse>
{
    public async Task<CreateHabitResponse> Handle(CreateHabitRequest request, CancellationToken ct)
    {
        var habit = new HabitEntity { Name = request.Name, Icon = request.Icon, UserId = currentUser.UserId, IsSystem = false };
        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync(ct);
        return new CreateHabitResponse(habit.Id);
    }
}

public class CreateHabitRequestValidator : AbstractValidator<CreateHabitRequest>
{
    public CreateHabitRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
