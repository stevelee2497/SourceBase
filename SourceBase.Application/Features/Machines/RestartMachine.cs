using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Machines;

public record RestartMachineRequest([property: FromRoute] Guid Id);

public record RestartMachineResponse(string Message);

public class RestartMachineEndpoint : IEndpoint
{
    public const string Route = "machines/{id:guid}/restart";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (Guid id, RestartMachineHandler handler, CancellationToken ct) => handler.Handle(new RestartMachineRequest(id), ct))
        .WithTags("Machines");
}

public class RestartMachineHandler(IDbContext dbContext, ICurrentUser currentUser, IMachineCommandService commandService) : IRequestHandler<RestartMachineRequest, RestartMachineResponse>
{
    public async Task<RestartMachineResponse> Handle(RestartMachineRequest request, CancellationToken ct)
    {
        var machine = await dbContext.Machines.FindAsync([request.Id], ct);
        if (machine == null || machine.UserId != currentUser.UserId)
            throw new NotFoundException();

        await commandService.SendCommandAsync(currentUser.UserId, machine.Id, MachineCommandType.Restart, ct);
        return new RestartMachineResponse("Restart command sent");
    }
}
