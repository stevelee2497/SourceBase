using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public record GetAuditsRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Desc) : PagingRequest(Page, Limit, Order, "ActionOn");

public record AuditHistoryResponse(Guid Id, string Author, string Action, string EntityType, string EntityId, JsonElement? Current, JsonElement? Original, JsonElement? Changes, DateTime ActionOn);

public class GetAuditsEndpoint : IEndpoint
{
    public const string Route = "data/audits";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetAuditsRequest request, GetAuditsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Data");
}

public class GetAuditsHandler(IDbContext dbContext) : IRequestHandler<GetAuditsRequest, PagingResponse<AuditHistoryResponse>>
{
    public async Task<PagingResponse<AuditHistoryResponse>> Handle(GetAuditsRequest request, CancellationToken ct)
    {
        var audits = await dbContext.AuditHistories.PaginateAsync(x => new AuditHistoryResponse(
            x.Id,
            x.Author!,
            x.Action,
            x.EntityType,
            x.EntityId,
            x.Current != null ? x.Current.Deserialize<JsonElement?>() : null,
            x.Original != null ? x.Original.Deserialize<JsonElement?>() : null,
            x.Changes != null ? x.Changes.Deserialize<JsonElement?>() : null,
            x.ActionOn
        ), request, ct);
        return audits;
    }
}
