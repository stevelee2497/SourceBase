using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public class GetAudits : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/audits", Handler).WithTags("Data").RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

    private async Task<Ok<List<AuditHistoryEntity>>> Handler(IDbContext dbContext, CancellationToken cancellationToken)
    {
        var audits = await dbContext.AuditHistories.ToListAsync(cancellationToken);
        return TypedResults.Ok(audits);
    }
}