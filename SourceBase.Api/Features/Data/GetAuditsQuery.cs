using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;

namespace SourceBase.Api.Features.Data;

public record GetAuditsQuery() : IRequest<List<AuditHistoryEntity>>;

public class GetAuditsQueryHandler(IDbContext dbContext) : IRequestHandler<GetAuditsQuery, List<AuditHistoryEntity>>
{
    public Task<List<AuditHistoryEntity>> Handle(GetAuditsQuery request, CancellationToken cancellationToken)
    {
        return dbContext.AuditHistories.ToListAsync(cancellationToken);
    }
}

public static class GetAuditsQueryEndpoint
{
    public static IEndpointRouteBuilder MapGetAuditsQueryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/audits", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetAuditsQuery(), cancellationToken)))
            .WithTags("Data")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

        return endpoints;
    }
}
