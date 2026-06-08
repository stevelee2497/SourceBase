using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.TimeSheets;

public record DeleteTimeSheetRequest(Guid Id);

public record DeleteTimeSheetResponse(bool Success);

public class DeleteTimeSheetEndpoint : IEndpoint
{
    public const string Route = "time-sheets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteTimeSheetHandler handler, CancellationToken ct) => handler.Handle(new DeleteTimeSheetRequest(id), ct))
        .WithTags("TimeSheets");
}

public class DeleteTimeSheetHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteTimeSheetRequest, DeleteTimeSheetResponse>
{
    public async Task<DeleteTimeSheetResponse> Handle(DeleteTimeSheetRequest request, CancellationToken ct)
    {
        var entity = await dbContext.TimeSheets.FindAsync([request.Id], ct);

        if (entity is null || entity.UserId != currentUser.UserId)
            throw new NotFoundException();

        dbContext.TimeSheets.Remove(entity);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTimeSheetResponse(true);
    }
}
