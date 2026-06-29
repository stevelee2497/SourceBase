using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Habits;

public record UpdateHabitRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string? Name, string? Icon);
public record UpdateHabitResponse(Guid Id);

public class UpdateHabitEndpoint : IEndpoint
{
    public const string Route = "habits/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateHabitRequest body, UpdateHabitHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Habits");
}

public class UpdateHabitHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateHabitRequest, UpdateHabitResponse>
{
    public async Task<UpdateHabitResponse> Handle(UpdateHabitRequest request, CancellationToken ct)
    {
        var habit = await dbContext.Habits.FindAsync([request.Id], ct);
        if (habit is null) throw new NotFoundException();
        if (habit.IsSystem) throw new ForbiddenException();
        if (habit.UserId != currentUser.UserId) throw new NotFoundException();

        habit.Name = request.Name ?? habit.Name;
        habit.Icon = request.Icon ?? habit.Icon;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateHabitResponse(habit.Id);
    }
}

public class UpdateHabitRequestValidator : AbstractValidator<UpdateHabitRequest>
{
    public UpdateHabitRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
    }
}
