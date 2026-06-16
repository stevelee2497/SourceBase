using FluentValidation;
using SourceBase.Application.Shared.Interfaces;


namespace SourceBase.Application.Features.Icons;

public record DeleteIconRequest(Guid Id);

public record DeleteIconResponse(bool Success);

public class DeleteIconEndpoint : IEndpoint
{
    public const string Route = "icons/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteIconRequest request, DeleteIconHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Icons");
}

public class DeleteIconHandler(IDbContext dbContext) : IRequestHandler<DeleteIconRequest, DeleteIconResponse>
{
    public async Task<DeleteIconResponse> Handle(DeleteIconRequest request, CancellationToken ct)
    {
        var icon = await dbContext.Icons.FindAsync([request.Id], ct);
        dbContext.Icons.Remove(icon!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteIconResponse(true);
    }
}

public class DeleteIconRequestValidator : AbstractValidator<DeleteIconRequest>
{
    public DeleteIconRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var icon = await dbContext.Icons.FindAsync([id], ct);
                return icon is not null;
            })
            .WithMessage("Icon not found.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var icon = await dbContext.Icons.FindAsync([id], ct);
                        return icon is not { IsSystem: true };
                    })
                    .WithMessage("Cannot delete a system icon.");
            });
    }
}
