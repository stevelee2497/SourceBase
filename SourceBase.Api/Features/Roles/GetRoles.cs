using System.Text.Json.Serialization;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Roles;

public class GetRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet("/roles", ([AsParameters] GetRolesRequest request, GetRolesHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Roles");
}

public class GetRolesHandler(IDbContext dbContext) : IRequestHandler<GetRolesRequest, PagingResponse<RoleResponse>>
{
    public async Task<PagingResponse<RoleResponse>> Handle(GetRolesRequest request, CancellationToken ct)
    {
        var response = await dbContext.Roles.PaginateAsync(role => new RoleResponse(role.Id, role.Name!, role.Description), request, ct);
        return response;
    }
}

public record GetRolesRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, RolesOrder? OrderBy = null) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public record RoleResponse(Guid Id, string Name, string? Description);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RolesOrder
{
    Name,
    Description,
    CreatedOn,
    CreatedBy,
    UpdatedOn,
    UpdatedBy
}
