using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Machines;

public record ShutdownMachineRequest([property: FromRoute] Guid Id);

public record ShutdownMachineResponse(string Message);

public class ShutdownMachineEndpoint : IEndpoint
{
    public const string Route = "machines/{id:guid}/shutdown";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (Guid id, ShutdownMachineHandler handler, CancellationToken ct) => handler.Handle(new ShutdownMachineRequest(id), ct))
        .WithTags("Machines");
}

public class ShutdownMachineHandler(IDbContext dbContext, ICurrentUser currentUser, IMachineCommandService commandService) : IRequestHandler<ShutdownMachineRequest, ShutdownMachineResponse>
{
    public async Task<ShutdownMachineResponse> Handle(ShutdownMachineRequest request, CancellationToken ct)
    {
        var machine = await dbContext.Machines.FindAsync([request.Id], ct);
        if (machine == null || machine.UserId != currentUser.UserId)
            throw new NotFoundException();

        await commandService.SendCommandAsync(currentUser.UserId, machine.Id, MachineCommandType.Shutdown, ct);
        return new ShutdownMachineResponse("Shutdown command sent");
    }
}
