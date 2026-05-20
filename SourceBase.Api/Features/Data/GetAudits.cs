using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public class GetAudits : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/audits", Handler).WithTags("Data").RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

    private async Task<Ok<PagingResponse<AuditHistoryEntity>>> Handler([AsParameters] GetAuditsRequest request, IDbContext dbContext, CancellationToken ct)
    {
        var audits = await dbContext.AuditHistories.PaginateAsync(x => x, request, ct);
        return TypedResults.Ok(audits);
    }
}

public record GetAuditsRequest : PagingRequest;