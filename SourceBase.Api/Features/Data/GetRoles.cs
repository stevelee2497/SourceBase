using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public class GetRoles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/roles", Handler).AllowAnonymous().WithTags("Data");

    private async Task<Ok<PagingResponse<RoleResponse>>> Handler([AsParameters] RoleRequest request, IDbContext dbContext, CancellationToken ct)
    {
        var response = await dbContext.Roles.PaginateAsync(role => new RoleResponse(role.Id, role.Name!), request, ct);
        return TypedResults.Ok(response);
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

public record RoleRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, RolesOrder? OrderBy = null) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public record RoleResponse(Guid Id, string Name);
