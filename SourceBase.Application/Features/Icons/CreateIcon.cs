using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Icons;

public record CreateIconRequest(string Value, string Name, IconGroup Group, int SortOrder);

public record CreateIconResponse(Guid Id);

public class CreateIconEndpoint : IEndpoint
{
    public const string Route = "icons";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateIconRequest request, CreateIconHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Icons");
}

public class CreateIconHandler(IDbContext dbContext) : IRequestHandler<CreateIconRequest, CreateIconResponse>
{
    public async Task<CreateIconResponse> Handle(CreateIconRequest request, CancellationToken ct)
    {
        var icon = new IconEntity
        {
            Value = request.Value,
            Name = request.Name,
            Group = request.Group,
            SortOrder = request.SortOrder,
            IsSystem = false,
        };
        dbContext.Icons.Add(icon);
        await dbContext.SaveChangesAsync(ct);
        return new CreateIconResponse(icon.Id);
    }
}

public class CreateIconRequestValidator : AbstractValidator<CreateIconRequest>
{
    public CreateIconRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
