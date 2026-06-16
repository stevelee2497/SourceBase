using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;


namespace SourceBase.Application.Features.Icons;

public record UpdateIconRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string? Value, string? Name, IconGroup? Group, int? SortOrder);

public record UpdateIconResponse(Guid Id);

public class UpdateIconEndpoint : IEndpoint
{
    public const string Route = "icons/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateIconRequest body, UpdateIconHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Icons");
}

public class UpdateIconHandler(IDbContext dbContext) : IRequestHandler<UpdateIconRequest, UpdateIconResponse>
{
    public async Task<UpdateIconResponse> Handle(UpdateIconRequest request, CancellationToken ct)
    {
        var icon = await dbContext.Icons.FindAsync([request.Id], ct)!;

        icon!.Value = request.Value ?? icon.Value;
        icon.Name = request.Name ?? icon.Name;
        icon.Group = request.Group ?? icon.Group;
        icon.SortOrder = request.SortOrder ?? icon.SortOrder;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateIconResponse(icon.Id);
    }
}

public class UpdateIconRequestValidator : AbstractValidator<UpdateIconRequest>
{
    public UpdateIconRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) => await dbContext.Icons.FindAsync([id], ct) is not null)
            .WithMessage("Icon not found.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var icon = await dbContext.Icons.FindAsync([id], ct);
                        return !icon!.IsSystem;
                    })
                    .WithMessage("System icon cannot be updated.");
            });

        RuleFor(x => x.Value).NotEmpty().When(x => x.Value is not null);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
    }
}
