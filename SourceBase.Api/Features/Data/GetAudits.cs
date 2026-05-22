using Microsoft.AspNetCore.Authorization;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public record GetAuditsRequest : PagingRequest;

public class GetAuditsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet("/data/audits", ([AsParameters] GetAuditsRequest request, GetAuditsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Data");
}

public class GetAuditsHandler(IDbContext dbContext) : IRequestHandler<GetAuditsRequest, PagingResponse<AuditHistoryEntity>>
{
    public async Task<PagingResponse<AuditHistoryEntity>> Handle(GetAuditsRequest request, CancellationToken ct)
    {
        var audits = await dbContext.AuditHistories.PaginateAsync(x => x, request, ct);
        return audits;
    }
}
