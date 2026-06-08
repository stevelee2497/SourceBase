using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Categories;

public record DeleteCategoryRequest(Guid Id);

public record DeleteCategoryResponse(bool Success);

public class DeleteCategoryEndpoint : IEndpoint
{
    public const string Route = "categories/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteCategoryHandler handler, CancellationToken ct) => handler.Handle(new DeleteCategoryRequest(id), ct))
        .WithTags("Categories");
}

public class DeleteCategoryHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteCategoryRequest, DeleteCategoryResponse>
{
    public async Task<DeleteCategoryResponse> Handle(DeleteCategoryRequest request, CancellationToken ct)
    {
        var category = await dbContext.Categories.FindAsync([request.Id], ct);
        if (category is null)
            throw new NotFoundException();
        if (category.IsSystem)
            throw new ForbiddenException();
        if (category.UserId != currentUser.UserId)
            throw new NotFoundException();

        var isInUse = await dbContext.Transactions.AnyAsync(t => t.CategoryId == request.Id, ct);
        if (isInUse)
            throw new ValidationException("Category is in use by transactions");

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteCategoryResponse(true);
    }
}
