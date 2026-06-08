using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Categories;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Categories;

public class GetCategoriesTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CATS-GET-001: GetCategories_WithoutToken_ReturnsUnauthorized")]
    public async Task GetCategories_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-GET-002: GetCategories_ReturnsSystemAndUserCategories")]
    public async Task GetCategories_ReturnsSystemAndUserCategories()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createCatResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createCatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var custom = await createCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await client.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.Should().Contain(x => x.Id == custom!.Id && !x.IsSystem);
        body.Should().Contain(x => x.IsSystem);
    }

    [Fact(DisplayName = "CATS-GET-003: GetCategories_WithMultipleUsers_ExcludesOtherUsersCategories")]
    public async Task GetCategories_WithMultipleUsers_ExcludesOtherUsersCategories()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownCatResponse = await ownerClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Own_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        ownCatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownCategory = await ownCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var otherCatResponse = await otherClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Other_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        otherCatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherCategory = await otherCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await ownerClient.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.Should().Contain(x => x.Id == ownCategory!.Id);
        body.Should().NotContain(x => x.Id == otherCategory!.Id);
    }

    [Fact(DisplayName = "CATS-GET-004: GetCategories_WithIncomeFilter_ReturnsOnlyIncomeCategories")]
    public async Task GetCategories_WithIncomeFilter_ReturnsOnlyIncomeCategories()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });

        // Act
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={CategoryType.Income}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.Should().NotBeEmpty();
        body.Should().OnlyContain(x => x.Type == CategoryType.Income);
    }

    [Fact(DisplayName = "CATS-GET-005: GetCategories_WithExpenseFilter_ReturnsOnlyExpenseCategories")]
    public async Task GetCategories_WithExpenseFilter_ReturnsOnlyExpenseCategories()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });

        // Act
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={CategoryType.Expense}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.Should().NotBeEmpty();
        body.Should().OnlyContain(x => x.Type == CategoryType.Expense);
    }
}
