using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Categories;

public record UpdateCategoryRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string? Name, string? Icon);

public record UpdateCategoryResponse(Guid Id);

public class UpdateCategoryEndpoint : IEndpoint
{
    public const string Route = "categories/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateCategoryRequest body, UpdateCategoryHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Categories");
}

public class UpdateCategoryHandler(IDbContext dbContext) : IRequestHandler<UpdateCategoryRequest, UpdateCategoryResponse>
{
    public async Task<UpdateCategoryResponse> Handle(UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await dbContext.Categories.FindAsync([request.Id], ct)!;

        category!.Name = request.Name ?? category.Name;
        category.Icon = request.Icon ?? category.Icon;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateCategoryResponse(category.Id);
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var category = await dbContext.Categories.FindAsync([id], ct);
                return category is not null && category.UserId == currentUser.UserId;
            })
            .WithMessage("Category not found.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var category = await dbContext.Categories.FindAsync([id], ct);
                        return !category!.IsSystem;
                    })
                    .WithMessage("System category cannot be updated.");
            });

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
    }
}
