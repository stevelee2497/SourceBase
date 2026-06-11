using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Icons;

public record GetIconsRequest([FromQuery] string? Group = null);

public record IconResponse(Guid Id, string Value, string Name, string Group, int SortOrder, bool IsSystem);

public class GetIconsEndpoint : IEndpoint
{
    public const string Route = "icons";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetIconsRequest request, GetIconsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Icons");
}

public class GetIconsHandler(IDbContext dbContext) : IRequestHandler<GetIconsRequest, List<IconResponse>>
{
    public async Task<List<IconResponse>> Handle(GetIconsRequest request, CancellationToken ct)
    {
        var parsed = Enum.TryParse<IconGroup>(request.Group, ignoreCase: true, out var groupEnum) ? (IconGroup?)groupEnum : null;

        return await dbContext.Icons
            .Where(i => parsed == null || i.Group == parsed || i.Group == IconGroup.General)
            .OrderBy(i => i.SortOrder)
            .Select(i => new IconResponse(i.Id, i.Value, i.Name, i.Group.ToString(), i.SortOrder, i.IsSystem))
            .ToListAsync(ct);
    }
}
