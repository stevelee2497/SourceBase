using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;


namespace SourceBase.Application.Features.Icons;

public record UpdateIconRequest([property: SwaggerIgnore] Guid Id, string Value, string Name, IconGroup Group, int SortOrder);

public record UpdateIconResponse(Guid Id);

public class UpdateIconEndpoint : IEndpoint
{
    public const string Route = "icons/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] UpdateIconRequest body, UpdateIconHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .WithTags("Icons");
}

public class UpdateIconHandler(IDbContext dbContext) : IRequestHandler<UpdateIconRequest, UpdateIconResponse>
{
    public async Task<UpdateIconResponse> Handle(UpdateIconRequest request, CancellationToken ct)
    {
        var icon = await dbContext.Icons.FindAsync([request.Id], ct);
        if (icon is null)
            throw new NotFoundException();
        if (icon.IsSystem)
            throw new ForbiddenException();

        icon.Value = request.Value;
        icon.Name = request.Name;
        icon.Group = request.Group;
        icon.SortOrder = request.SortOrder;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateIconResponse(icon.Id);
    }
}

public class UpdateIconRequestValidator : AbstractValidator<UpdateIconRequest>
{
    public UpdateIconRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
