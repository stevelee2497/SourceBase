using System.Text.Json.Serialization;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;

namespace SourceBase.Application.Features.TimeSheets;

public record GetTimeSheetRequest(Guid Id);

[method: JsonConstructor]
public record GetTimeSheetResponse(Guid Id, DateOnly Date, string Project, decimal Hours, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public GetTimeSheetResponse(TimeSheetEntity e) : this(e.Id, e.Date, e.Project, e.Hours, e.CreatedOn, e.CreatedBy, e.UpdatedOn, e.UpdatedBy)
    {
    }
}

public class GetTimeSheetEndpoint : IEndpoint
{
    public const string Route = "time-sheets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (Guid id, GetTimeSheetHandler handler, CancellationToken ct) => handler.Handle(new GetTimeSheetRequest(id), ct))
        .WithTags("TimeSheets");
}

public class GetTimeSheetHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTimeSheetRequest, GetTimeSheetResponse>
{
    public async Task<GetTimeSheetResponse> Handle(GetTimeSheetRequest request, CancellationToken ct)
    {
        var entity = await dbContext.TimeSheets.FindAsync([request.Id], ct);

        if (entity is null || entity.UserId != currentUser.UserId)
            throw new NotFoundException();

        return new GetTimeSheetResponse(entity);
    }
}
