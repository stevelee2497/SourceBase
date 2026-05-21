using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public class GetAuditsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet("/data/audits", ([AsParameters] GetAuditsRequest request, GetAuditsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Data");
}

public class GetAuditsHandler(IDbContext dbContext) : IRequestHandler<GetAuditsRequest, Ok<PagingResponse<AuditHistoryEntity>>>
{
    public async Task<Ok<PagingResponse<AuditHistoryEntity>>> Handle(GetAuditsRequest request, CancellationToken ct)
    {
        var audits = await dbContext.AuditHistories.PaginateAsync(x => x, request, ct);
        return TypedResults.Ok(audits);
    }
}

public record GetAuditsRequest : PagingRequest;
