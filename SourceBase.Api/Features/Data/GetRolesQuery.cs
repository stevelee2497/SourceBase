using MediatR;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Data;

public record GetRolesQuery() : IRequest<List<RoleResponse>>;

public class GetRolesQueryHandler(IDbContext dbContext) : IRequestHandler<GetRolesQuery, List<RoleResponse>>
{
    public Task<List<RoleResponse>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Roles.Select(role => new RoleResponse(role.Id, role.Name!)).ToListAsync(cancellationToken);
    }
}

public record RoleResponse(Guid Id, string Name);

public class GetRolesQueryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/roles", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetRolesQuery(), cancellationToken)))
            .AllowAnonymous()
            .WithTags("Data");
}
