using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Categories;

[EndpointFact(
    Feature = "Categories",
    Name = "Delete Category",
    Route = "DELETE /api/categories/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to delete one of my custom categories, so that I can remove categories I no longer use.",
    Description = new[]
    {
        "Client provides the category `id` (route).",
        "If the category doesn't exist or belongs to a different user → `404 Not Found`.",
        "If the category is a system category → `403 Forbidden`.",
        "If any transaction references this category → `400 Bad Request` with message `\"Category is in use by transactions\"`.",
        "The category is deleted.",
    })]
public class DeleteCategoryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CATS-DELETE-001: DeleteCategory_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteCategory_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-DELETE-002: DeleteCategory_WithOwnedUnusedCategory_ReturnsOk")]
    public async Task DeleteCategory_WithOwnedUnusedCategory_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteCategoryResponse>();
        body!.Success.ShouldBeTrue();

        var categoriesResponse = await client.GetAsync(GetCategoriesEndpoint.Route);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories!.ShouldNotContain(x => x.Id == create.Id);
    }

    [Fact(DisplayName = "CATS-DELETE-003: DeleteCategory_WithSystemCategory_ReturnsForbidden")]
    public async Task DeleteCategory_WithSystemCategory_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var catListResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        catListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var categories = await catListResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var systemCategory = categories!.First(x => x.IsSystem);

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(systemCategory.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CATS-DELETE-004: DeleteCategory_WithOtherUsersCategory_ReturnsNotFound")]
    public async Task DeleteCategory_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-DELETE-005: DeleteCategory_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteCategory_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-DELETE-006: DeleteCategory_WithReferencedTransactions_ReturnsBadRequest")]
    public async Task DeleteCategory_WithReferencedTransactions_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        catResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var category = await catResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Expense", date = "2025-03-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = category!.Id });
        txnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(category.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Category is in use by transactions");
    }
}
