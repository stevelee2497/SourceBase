using MediatR;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Data;

public record GetRolesQuery() : IRequest<List<RoleResponse>>;

public class GetRolesQueryHandler(IDbContext dbContext) : IRequestHandler<GetRolesQuery, List<RoleResponse>>
{
    public Task<List<RoleResponse>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Roles.Select(x => new RoleResponse(x.Id, x.Name!)).ToListAsync(cancellationToken);
    }
}

public record RoleResponse(
    Guid Id,
    string Name);
