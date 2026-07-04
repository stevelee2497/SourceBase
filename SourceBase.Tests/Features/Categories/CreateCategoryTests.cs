using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Categories;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Categories;

[EndpointFact(
    Feature = "Categories",
    Name = "Create Category",
    Route = "POST /api/categories",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to create a custom transaction category, so that I can organise my transactions beyond the default categories.",
    Description = new[]
    {
        "Client sends `name` (required, max 100 characters), `type` (required: `Income` or `Expense`), and optionally `icon`.",
        "The category is created as a user-owned (non-system) category.",
        "Returns the new category's `Id`.",
    })]
public class CreateCategoryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CATS-CREATE-001: CreateCategory_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = $"Unauthorized_{Guid.NewGuid():N}",
            type = CategoryType.Expense.ToString(),
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-CREATE-002: CreateCategory_WithValidData_ReturnsOkAndId")]
    public async Task CreateCategory_WithValidData_ReturnsOkAndId()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = $"Created_{Guid.NewGuid():N}",
            type = CategoryType.Expense.ToString(),
            icon = "🧾",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "CATS-CREATE-003: CreateCategory_WithMissingName_ReturnsBadRequest")]
    public async Task CreateCategory_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            type = CategoryType.Expense.ToString(),
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-CREATE-004: CreateCategory_WithMissingType_ReturnsBadRequest")]
    public async Task CreateCategory_WithMissingType_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = $"NoType_{Guid.NewGuid():N}",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-CREATE-005: CreateCategory_WithInvalidType_ReturnsBadRequest")]
    public async Task CreateCategory_WithInvalidType_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = $"InvalidType_{Guid.NewGuid():N}",
            type = "InvalidType",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-CREATE-006: CreateCategory_WithAuthenticatedUser_SetsOwnershipAndNonSystem")]
    public async Task CreateCategory_WithAuthenticatedUser_SetsOwnershipAndNonSystem()
    {
        // Arrange
        var email = $"category_owner_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");

        // Act
        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Assert
        var catResponse = await client.GetAsync(GetCategoriesEndpoint.Route);
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.Single(x => x.Id == create!.Id);
        category.IsSystem.ShouldBeFalse();
    }
}
