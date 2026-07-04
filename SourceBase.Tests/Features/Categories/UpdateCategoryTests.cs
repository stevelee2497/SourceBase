using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Categories;

[EndpointFact(
    Feature = "Categories",
    Name = "Update Category",
    Route = "PATCH /api/categories/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to partially update a custom category's name or icon, so that I can keep my organisation up to date without resending all fields.",
    Description = new[]
    {
        "Client sends the category `id` (route) and any subset of: `name`, `icon`. All fields are optional — only provided (non-null) fields are updated.",
        "If the category doesn't exist or belongs to a different user → `404 Not Found`.",
        "If the category is a system category (`IsSystem = true`) → `403 Forbidden`.",
        "If `name` is provided but empty → `400 Bad Request`.",
        "Returns the updated category's `Id`.",
    })]
public class UpdateCategoryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CATS-UPDATE-001: missing token returns 401")]
    public async Task UpdateCategory_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Updated_{Guid.NewGuid():N}",
            icon = "✏️",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-UPDATE-002: valid data returns 200")]
    public async Task UpdateCategory_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();
        var updatedName = $"Updated_{Guid.NewGuid():N}";

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            name = updatedName,
            icon = "🏷️",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateCategoryResponse>();
        body!.Id.ShouldBe(create.Id);

        var categoriesResponse = await client.GetAsync(GetCategoriesEndpoint.Route);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var updated = categories!.Single(x => x.Id == create.Id);
        updated.Name.ShouldBe(updatedName);
        updated.Icon.ShouldBe("🏷️");
    }

    [Fact(DisplayName = "CATS-UPDATE-003: only icon returns 200 and keeps name")]
    public async Task UpdateCategory_WithOnlyIcon_ReturnsOkAndKeepsName()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var originalName = $"Category_{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = originalName, type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            icon = "✏️",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var categoriesResponse = await client.GetAsync(GetCategoriesEndpoint.Route);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var updated = categories!.Single(x => x.Id == create.Id);
        updated.Name.ShouldBe(originalName);
        updated.Icon.ShouldBe("✏️");
    }

    [Fact(DisplayName = "CATS-UPDATE-004: system category returns 403")]
    public async Task UpdateCategory_WithSystemCategory_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var catListResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        catListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var categories = await catListResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var systemCategory = categories!.First(x => x.IsSystem);

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(systemCategory.Id), new
        {
            name = $"System_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CATS-UPDATE-005: other user's category returns 404")]
    public async Task UpdateCategory_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await otherClient.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            name = $"OtherUser_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-UPDATE-006: unknown id returns 404")]
    public async Task UpdateCategory_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Unknown_{Guid.NewGuid():N}",
            icon = "🏷️",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-UPDATE-007: empty id returns 400")]
    public async Task UpdateCategory_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(Guid.Empty), new
        {
            name = "Test",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
