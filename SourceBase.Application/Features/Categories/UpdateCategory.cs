using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Categories;

public record UpdateCategoryRequest([property: SwaggerIgnore] Guid Id, string? Name, string? Icon);

public record UpdateCategoryResponse(Guid Id);

public class UpdateCategoryEndpoint : IEndpoint
{
    public const string Route = "categories/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, (Guid id, [FromBody] UpdateCategoryRequest body, UpdateCategoryHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .WithTags("Categories");
}

public class UpdateCategoryHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateCategoryRequest, UpdateCategoryResponse>
{
    public async Task<UpdateCategoryResponse> Handle(UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await dbContext.Categories.FindAsync([request.Id], ct);
        if (category is null)
            throw new NotFoundException();
        if (category.IsSystem)
            throw new ForbiddenException();
        if (category.UserId != currentUser.UserId)
            throw new NotFoundException();

        category.Name = request.Name ?? category.Name;
        category.Icon = request.Icon ?? category.Icon;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateCategoryResponse(category.Id);
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
    }
}
