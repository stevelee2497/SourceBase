using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Categories;

public record CreateCategoryRequest(string Name, CategoryType? Type, string? Icon);

public record CreateCategoryResponse(Guid Id);

public class CreateCategoryEndpoint : IEndpoint
{
    public const string Route = "categories";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateCategoryRequest request, CreateCategoryHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Categories");
}

public class CreateCategoryHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateCategoryRequest, CreateCategoryResponse>
{
    public async Task<CreateCategoryResponse> Handle(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = new CategoryEntity
        {
            Name = request.Name,
            Type = request.Type!.Value,
            Icon = request.Icon,
            UserId = currentUser.UserId,
            IsSystem = false,
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(ct);
        return new CreateCategoryResponse(category.Id);
    }
}

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotNull();
    }
}
