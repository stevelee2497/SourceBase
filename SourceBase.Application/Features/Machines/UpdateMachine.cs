using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Machines;

public record UpdateMachineRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string? Alias, MachineStatus? Status);

public record UpdateMachineResponse(Guid Id);

public class UpdateMachineEndpoint : IEndpoint
{
    public const string Route = "machines/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateMachineRequest body, UpdateMachineHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Machines");
}

public class UpdateMachineHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateMachineRequest, UpdateMachineResponse>
{
    public async Task<UpdateMachineResponse> Handle(UpdateMachineRequest request, CancellationToken ct)
    {
        var machine = await dbContext.Machines.FindAsync([request.Id], ct);
        if (machine == null || machine.UserId != currentUser.UserId)
            throw new NotFoundException();

        machine.Alias = request.Alias ?? machine.Alias;
        machine.Status = request.Status ?? machine.Status;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateMachineResponse(machine.Id);
    }
}

public class UpdateMachineRequestValidator : AbstractValidator<UpdateMachineRequest>
{
    public UpdateMachineRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Alias).NotEmpty().When(x => x.Alias is not null);
    }
}
