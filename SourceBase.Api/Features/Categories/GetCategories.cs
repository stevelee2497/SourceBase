using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Categories;

public record GetCategoriesRequest(CategoryType? Type = null);

public record CategoryResponse(Guid Id, string Name, CategoryType Type, string? Icon, bool IsSystem);

public class GetCategoriesEndpoint : IEndpoint
{
    public const string Route = "categories";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetCategoriesRequest request, GetCategoriesHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Categories");
}

public class GetCategoriesHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetCategoriesRequest, List<CategoryResponse>>
{
    public async Task<List<CategoryResponse>> Handle(GetCategoriesRequest request, CancellationToken ct)
    {
        return await dbContext.Categories
            .Where(c => (c.IsSystem || c.UserId == currentUser.UserId) && (request.Type == null || c.Type == request.Type))
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Type, c.Icon, c.IsSystem))
            .ToListAsync(ct);
    }
}
