using System.Text.Json.Serialization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Machines;

public record GetMachinesRequest(int? Page, int? Limit, PagingOrder? Order, GetMachinesOrder? OrderBy) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

[method: JsonConstructor]
public record GetMachineResponse(Guid Id, string Name, string? Alias, MachineStatus Status, DateTime? LastReportedOn)
{
    public GetMachineResponse(MachineEntity machine) : this(machine.Id, machine.Name, machine.Alias, machine.Status, machine.LastReportedOn) { }
}

public class GetMachinesEndpoint : IEndpoint
{
    public const string Route = "machines";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetMachinesRequest request, GetMachinesHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Machines");
}

public class GetMachinesHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetMachinesRequest, PagingResponse<GetMachineResponse>>
{
    public async Task<PagingResponse<GetMachineResponse>> Handle(GetMachinesRequest request, CancellationToken ct)
    {
        var machines = await dbContext.Machines
            .Where(x => x.UserId == currentUser.UserId)
            .PaginateAsync(x => new GetMachineResponse(x), request, ct);
        return machines;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GetMachinesOrder
{
    Name,
    Status,
    LastReportedOn,
    CreatedOn,
    UpdatedOn
}
