using FluentValidation;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.TimeSheets;

public record DeleteTimeSheetRequest(Guid Id);

public record DeleteTimeSheetResponse(bool Success);

public class DeleteTimeSheetEndpoint : IEndpoint
{
    public const string Route = "time-sheets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteTimeSheetRequest request, DeleteTimeSheetHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TimeSheets");
}

public class DeleteTimeSheetHandler(IDbContext dbContext) : IRequestHandler<DeleteTimeSheetRequest, DeleteTimeSheetResponse>
{
    public async Task<DeleteTimeSheetResponse> Handle(DeleteTimeSheetRequest request, CancellationToken ct)
    {
        var entity = await dbContext.TimeSheets.FindAsync([request.Id], ct);
        dbContext.TimeSheets.Remove(entity!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTimeSheetResponse(true);
    }
}

public class DeleteTimeSheetRequestValidator : AbstractValidator<DeleteTimeSheetRequest>
{
    public DeleteTimeSheetRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
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
