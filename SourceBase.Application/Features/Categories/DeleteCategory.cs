using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using FluentValidation;

namespace SourceBase.Application.Features.Categories;

public record DeleteCategoryRequest(Guid Id);

public record DeleteCategoryResponse(bool Success);

public class DeleteCategoryEndpoint : IEndpoint
{
    public const string Route = "categories/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteCategoryRequest request, DeleteCategoryHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Categories");
}

public class DeleteCategoryHandler(IDbContext dbContext) : IRequestHandler<DeleteCategoryRequest, DeleteCategoryResponse>
{
    public async Task<DeleteCategoryResponse> Handle(DeleteCategoryRequest request, CancellationToken ct)
    {
        var category = await dbContext.Categories.FindAsync([request.Id], ct);
        dbContext.Categories.Remove(category!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteCategoryResponse(true);
    }
}

public class DeleteCategoryRequestValidator : AbstractValidator<DeleteCategoryRequest>
{
    public DeleteCategoryRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
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
                    .WithMessage("System category cannot be deleted.");
            })
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var hasTransactions = await dbContext.Transactions.AnyAsync(t => t.CategoryId == id, ct);
                        return !hasTransactions;
                    })
                    .WithMessage("Category is in use by transactions");
            });
    }
}