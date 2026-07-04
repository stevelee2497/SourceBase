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
    Name = "Get Categories",
    Route = "GET /api/categories",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve the list of transaction categories, so that I can assign categories when creating transactions.",
    Description = new[]
    {
        "Client calls the endpoint with optional `type` filter (`Income` or `Expense`).",
        "Returns system-default categories (seeded, `IsSystem = true`) plus the current user's custom categories.",
        "Each category includes `id`, `name`, `type`, `icon`, `isSystem`.",
    })]
public class GetCategoriesTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CATS-GET-001: without token returns 401")]
    public async Task GetCategories_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-GET-002: returns system and user categories")]
    public async Task GetCategories_ReturnsSystemAndUserCategories()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createCatResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createCatResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var custom = await createCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await client.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.ShouldContain(x => x.Id == custom!.Id && !x.IsSystem);
        body!.ShouldContain(x => x.IsSystem);
    }

    [Fact(DisplayName = "CATS-GET-003: multiple users exclude others' categories")]
    public async Task GetCategories_WithMultipleUsers_ExcludesOtherUsersCategories()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownCatResponse = await ownerClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Own_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        ownCatResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ownCategory = await ownCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var otherCatResponse = await otherClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Other_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        otherCatResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var otherCategory = await otherCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await ownerClient.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.ShouldContain(x => x.Id == ownCategory!.Id);
        body!.ShouldNotContain(x => x.Id == otherCategory!.Id);
    }

    [Fact(DisplayName = "CATS-GET-004: income filter returns only income categories")]
    public async Task GetCategories_WithIncomeFilter_ReturnsOnlyIncomeCategories()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });

        // Act
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={CategoryType.Income}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.ShouldNotBeEmpty();
        body.ShouldAllBe(x => x.Type == CategoryType.Income);
    }

    [Fact(DisplayName = "CATS-GET-005: expense filter returns only expense categories")]
    public async Task GetCategories_WithExpenseFilter_ReturnsOnlyExpenseCategories()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });

        // Act
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={CategoryType.Expense}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.ShouldNotBeEmpty();
        body.ShouldAllBe(x => x.Type == CategoryType.Expense);
    }
}
