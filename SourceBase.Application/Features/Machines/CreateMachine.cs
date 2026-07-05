using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Machines;

/// <summary>
/// Create or update a machine. If a machine with the same name exists for the user, updates its status and LastReportedOn.
/// Otherwise, creates a new machine. Status is required for upsert semantics.
/// </summary>
public record CreateMachineRequest(string Name, MachineStatus? Status = null);

public record CreateMachineResponse(Guid Id);

public class CreateMachineEndpoint : IEndpoint
{
    public const string Route = "machines";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateMachineRequest request, CreateMachineHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Machines");
}

public class CreateMachineHandler(IDbContext dbContext, ICurrentUser currentUser, IDateTime dateTime) : IRequestHandler<CreateMachineRequest, CreateMachineResponse>
{
    public async Task<CreateMachineResponse> Handle(CreateMachineRequest request, CancellationToken ct)
    {
        var machine = await dbContext.Machines.FirstOrDefaultAsync(x => x.UserId == currentUser.UserId && x.Name == request.Name, ct);

        if (machine == null)
        {
            machine = new MachineEntity { Name = request.Name, UserId = currentUser.UserId, Status = request.Status ?? MachineStatus.Active, LastReportedOn = dateTime.UtcNow };
            dbContext.Machines.Add(machine);
        }
        else
        {
            machine.Status = request.Status ?? machine.Status;
            machine.LastReportedOn = dateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
        return new CreateMachineResponse(machine.Id);
    }
}

public class CreateMachineRequestValidator : AbstractValidator<CreateMachineRequest>
{
    public CreateMachineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
