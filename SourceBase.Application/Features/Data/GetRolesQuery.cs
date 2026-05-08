using MediatR;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Data;

public record GetRolesQuery() : IRequest<List<RoleResponse>>;

public class GetRolesQueryHandler(IIdentityService identityService) : IRequestHandler<GetRolesQuery, List<RoleResponse>>
{
    public Task<List<RoleResponse>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return identityService.GetRolesAsync(cancellationToken);
    }
}

public record RoleResponse(Guid Id, string Name);
