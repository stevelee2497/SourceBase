using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Categories;

public class UpdateCategoryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CATS-UPDATE-001: UpdateCategory_WithoutToken_ReturnsUnauthorized")]
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-UPDATE-002: UpdateCategory_WithValidData_ReturnsOk")]
    public async Task UpdateCategory_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();
        var updatedName = $"Updated_{Guid.NewGuid():N}";

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            name = updatedName,
            icon = "🏷️",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateCategoryResponse>();
        body!.Id.Should().Be(create.Id);

        var categoriesResponse = await client.GetAsync(GetCategoriesEndpoint.Route);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var updated = categories!.Single(x => x.Id == create.Id);
        updated.Name.Should().Be(updatedName);
        updated.Icon.Should().Be("🏷️");
    }

    [Fact(DisplayName = "CATS-UPDATE-003: UpdateCategory_WithOnlyIcon_ReturnsOkAndKeepsName")]
    public async Task UpdateCategory_WithOnlyIcon_ReturnsOkAndKeepsName()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var originalName = $"Category_{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = originalName, type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            icon = "✏️",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoriesResponse = await client.GetAsync(GetCategoriesEndpoint.Route);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var updated = categories!.Single(x => x.Id == create.Id);
        updated.Name.Should().Be(originalName);
        updated.Icon.Should().Be("✏️");
    }

    [Fact(DisplayName = "CATS-UPDATE-004: UpdateCategory_WithSystemCategory_ReturnsForbidden")]
    public async Task UpdateCategory_WithSystemCategory_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var catListResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        catListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await catListResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var systemCategory = categories!.First(x => x.IsSystem);

        // Act
        var response = await client.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(systemCategory.Id), new
        {
            name = $"System_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-UPDATE-005: UpdateCategory_WithOtherUsersCategory_ReturnsNotFound")]
    public async Task UpdateCategory_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await otherClient.PatchAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            name = $"OtherUser_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-UPDATE-006: UpdateCategory_WithUnknownId_ReturnsNotFound")]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
