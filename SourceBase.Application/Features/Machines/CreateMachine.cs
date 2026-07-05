using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Machines;

public record CreateMachineRequest(string Name);

public record CreateMachineResponse(Guid Id);

public class CreateMachineEndpoint : IEndpoint
{
    public const string Route = "machines";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateMachineRequest request, CreateMachineHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Machines");
}

public class CreateMachineHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateMachineRequest, CreateMachineResponse>
{
    public async Task<CreateMachineResponse> Handle(CreateMachineRequest request, CancellationToken ct)
    {
        var machine = new MachineEntity { Name = request.Name, UserId = currentUser.UserId, Status = MachineStatus.Active };
        dbContext.Machines.Add(machine);
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
