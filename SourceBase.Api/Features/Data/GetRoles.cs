using System.Text.Json.Serialization;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public class GetRoles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/roles", ([AsParameters] GetRolesRequest request, ISender sender, CancellationToken ct) => sender.Send(request, ct)).AllowAnonymous().WithTags("Data");
}

public class GetRolesHandler(IDbContext dbContext) : IRequestHandler<GetRolesRequest, PagingResponse<RoleResponse>>
{
    public async Task<PagingResponse<RoleResponse>> Handle(GetRolesRequest request, CancellationToken ct)
    {
        var response = await dbContext.Roles.PaginateAsync(role => new RoleResponse(role.Id, role.Name!), request, ct);
        return response;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RolesOrder
{
    Name,
    CreatedOn,
    CreatedBy,
    UpdatedOn,
    UpdatedBy
}

public record GetRolesRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, RolesOrder? OrderBy = null) : PagingRequest(Page, Limit, Order, OrderBy?.ToString()), IRequest<PagingResponse<RoleResponse>>;

public record RoleResponse(Guid Id, string Name);
