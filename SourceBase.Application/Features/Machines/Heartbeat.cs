using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Machines;

public record HeartbeatRequest(string Name, MachineStatus Status);

public record HeartbeatResponse(Guid Id);

public class HeartbeatEndpoint : IEndpoint
{
    public const string Route = "machines/heartbeat";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] HeartbeatRequest request, HeartbeatHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Machines");
}

public class HeartbeatHandler(IDbContext dbContext, ICurrentUser currentUser, IDateTime dateTime) : IRequestHandler<HeartbeatRequest, HeartbeatResponse>
{
    public async Task<HeartbeatResponse> Handle(HeartbeatRequest request, CancellationToken ct)
    {
        var machine = await dbContext.Machines.FirstOrDefaultAsync(x => x.UserId == currentUser.UserId && x.Name == request.Name, ct);

        if (machine == null)
        {
            machine = new MachineEntity { Name = request.Name, UserId = currentUser.UserId, Status = request.Status, LastReportedOn = dateTime.UtcNow };
            dbContext.Machines.Add(machine);
        }
        else
        {
            machine.Status = request.Status;
            machine.LastReportedOn = dateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
        return new HeartbeatResponse(machine.Id);
    }
}

public class HeartbeatRequestValidator : AbstractValidator<HeartbeatRequest>
{
    public HeartbeatRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
