using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Machines;

public record DeleteMachineRequest(Guid Id);

public record DeleteMachineResponse(Guid Id);

public class DeleteMachineEndpoint : IEndpoint
{
    public const string Route = "machines/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteMachineHandler handler, CancellationToken ct) => handler.Handle(new DeleteMachineRequest(id), ct))
        .WithTags("Machines");
}

public class DeleteMachineHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteMachineRequest, DeleteMachineResponse>
{
    public async Task<DeleteMachineResponse> Handle(DeleteMachineRequest request, CancellationToken ct)
    {
        var machine = await dbContext.Machines.FindAsync([request.Id], ct);
        if (machine == null || machine.UserId != currentUser.UserId)
            throw new NotFoundException();

        dbContext.Machines.Remove(machine);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteMachineResponse(machine.Id);
    }
}
