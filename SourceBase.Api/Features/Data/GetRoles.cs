using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public class GetRoles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/roles", Handler).AllowAnonymous().WithTags("Data");

    private async Task<Ok<List<RoleResponse>>> Handler(IDbContext dbContext, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.Select(role => new RoleResponse(role.Id, role.Name!)).ToListAsync(cancellationToken);
        return TypedResults.Ok(roles);
    }
}

public record RoleResponse(Guid Id, string Name);
