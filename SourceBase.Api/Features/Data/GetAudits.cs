using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public class GetAudits : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/audits",
        ([AsParameters] GetAuditsRequest request, ISender sender, CancellationToken ct) => sender.Send(request, ct)).WithTags("Data").RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });
}

public class GetAuditsHandler(IDbContext dbContext) : IRequestHandler<GetAuditsRequest, Ok<PagingResponse<AuditHistoryEntity>>>
{
    public async Task<Ok<PagingResponse<AuditHistoryEntity>>> Handle(GetAuditsRequest request, CancellationToken ct)
    {
        var audits = await dbContext.AuditHistories.PaginateAsync(x => x, request, ct);
        return TypedResults.Ok(audits);
    }
}

public record GetAuditsRequest : PagingRequest, IRequest<Ok<PagingResponse<AuditHistoryEntity>>>;