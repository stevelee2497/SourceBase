using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;


namespace SourceBase.Application.Features.Icons;

public record DeleteIconRequest(Guid Id);

public record DeleteIconResponse(bool Success);

public class DeleteIconEndpoint : IEndpoint
{
    public const string Route = "icons/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteIconHandler handler, CancellationToken ct) => handler.Handle(new DeleteIconRequest(id), ct))
        .WithTags("Icons");
}

public class DeleteIconHandler(IDbContext dbContext) : IRequestHandler<DeleteIconRequest, DeleteIconResponse>
{
    public async Task<DeleteIconResponse> Handle(DeleteIconRequest request, CancellationToken ct)
    {
        var icon = await dbContext.Icons.FindAsync([request.Id], ct);
        if (icon is null)
            throw new NotFoundException();
        if (icon.IsSystem)
            throw new ForbiddenException();

        dbContext.Icons.Remove(icon);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteIconResponse(true);
    }
}
