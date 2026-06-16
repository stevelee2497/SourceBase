using FluentValidation;
using System.Text.Json.Serialization;
using SourceBase.Application.Shared.Interfaces;

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
        .MapGet(Route, ([AsParameters] GetTimeSheetRequest request, GetTimeSheetHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TimeSheets");
}

public class GetTimeSheetHandler(IDbContext dbContext) : IRequestHandler<GetTimeSheetRequest, GetTimeSheetResponse>
{
    public async Task<GetTimeSheetResponse> Handle(GetTimeSheetRequest request, CancellationToken ct)
    {
        var entity = await dbContext.TimeSheets.FindAsync([request.Id], ct);
        return new GetTimeSheetResponse(entity!);
    }
}

public class GetTimeSheetRequestValidator : AbstractValidator<GetTimeSheetRequest>
{
    public GetTimeSheetRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var entity = await dbContext.TimeSheets.FindAsync([id], ct);
                return entity is not null && entity.UserId == currentUser.UserId;
            })
            .WithMessage("Time sheet not found.");
    }
}
