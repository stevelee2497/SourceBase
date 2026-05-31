using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Categories;
using SourceBase.Api.Features.Transactions;
using SourceBase.Api.Features.Wallets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Categories;

public class CategoryCrudTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
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
        var client = await CreateUserClientAsync();
        var custom = await CreateCategoryAsync(client, type: CategoryType.Expense);

        // Act
        var response = await client.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.Should().Contain(x => x.Id == custom.Id && !x.IsSystem);
        body.Should().Contain(x => x.IsSystem);
    }

    [Fact(DisplayName = "CATS-GET-003: GetCategories_WithMultipleUsers_ExcludesOtherUsersCategories")]
    public async Task GetCategories_WithMultipleUsers_ExcludesOtherUsersCategories()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var ownCategory = await CreateCategoryAsync(ownerClient, name: UniqueCategoryName("Own"), type: CategoryType.Income);
        var otherCategory = await CreateCategoryAsync(otherClient, name: UniqueCategoryName("Other"), type: CategoryType.Expense);

        // Act
        var response = await ownerClient.GetAsync(GetCategoriesEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.Should().Contain(x => x.Id == ownCategory.Id);
        body.Should().NotContain(x => x.Id == otherCategory.Id);
    }

    [Fact(DisplayName = "CATS-GET-004: GetCategories_WithIncomeFilter_ReturnsOnlyIncomeCategories")]
    public async Task GetCategories_WithIncomeFilter_ReturnsOnlyIncomeCategories()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        await CreateCategoryAsync(client, type: CategoryType.Income);
        await CreateCategoryAsync(client, type: CategoryType.Expense);

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
        var client = await CreateUserClientAsync();
        await CreateCategoryAsync(client, type: CategoryType.Income);
        await CreateCategoryAsync(client, type: CategoryType.Expense);

        // Act
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={CategoryType.Expense}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body!.Should().NotBeEmpty();
        body.Should().OnlyContain(x => x.Type == CategoryType.Expense);
    }

    [Fact(DisplayName = "CATS-CREATE-001: CreateCategory_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = UniqueCategoryName("Unauthorized"),
            type = CategoryType.Expense.ToString(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-CREATE-002: CreateCategory_WithValidData_ReturnsOkAndId")]
    public async Task CreateCategory_WithValidData_ReturnsOkAndId()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = UniqueCategoryName("Created"),
            type = CategoryType.Expense.ToString(),
            icon = "🧾",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "CATS-CREATE-003: CreateCategory_WithMissingName_ReturnsBadRequest")]
    public async Task CreateCategory_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            type = CategoryType.Expense.ToString(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-CREATE-004: CreateCategory_WithMissingType_ReturnsBadRequest")]
    public async Task CreateCategory_WithMissingType_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = UniqueCategoryName("NoType"),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-CREATE-005: CreateCategory_WithInvalidType_ReturnsBadRequest")]
    public async Task CreateCategory_WithInvalidType_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = UniqueCategoryName("InvalidType"),
            type = "InvalidType",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-CREATE-006: CreateCategory_WithAuthenticatedUser_SetsOwnershipAndNonSystem")]
    public async Task CreateCategory_WithAuthenticatedUser_SetsOwnershipAndNonSystem()
    {
        // Arrange
        var email = $"category_owner_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");

        // Act
        var create = await CreateCategoryAsync(client, type: CategoryType.Income);

        // Assert
        var data = await factory.WithDbContextAsync(async db => new
        {
            Category = await db.Categories.SingleAsync(x => x.Id == create.Id),
            UserId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync()
        });
        data.Category.UserId.Should().Be(data.UserId);
        data.Category.IsSystem.Should().BeFalse();
    }

    [Fact(DisplayName = "CATS-UPDATE-001: UpdateCategory_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateCategory_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = UniqueCategoryName("Updated"),
            icon = "✏️",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-UPDATE-002: UpdateCategory_WithValidData_ReturnsOk")]
    public async Task UpdateCategory_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateCategoryAsync(client, type: CategoryType.Expense);
        var updatedName = UniqueCategoryName("Updated");

        // Act
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create.Id), new
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

    [Fact(DisplayName = "CATS-UPDATE-003: UpdateCategory_WithMissingName_ReturnsBadRequest")]
    public async Task UpdateCategory_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateCategoryAsync(client, type: CategoryType.Expense);

        // Act
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create.Id), new
        {
            icon = "🏷️",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-UPDATE-004: UpdateCategory_WithSystemCategory_ReturnsForbidden")]
    public async Task UpdateCategory_WithSystemCategory_ReturnsForbidden()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var systemCategory = await GetSystemCategoryAsync(client, CategoryType.Expense);

        // Act
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(systemCategory.Id), new
        {
            name = UniqueCategoryName("System"),
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CATS-UPDATE-005: UpdateCategory_WithOtherUsersCategory_ReturnsNotFound")]
    public async Task UpdateCategory_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var create = await CreateCategoryAsync(ownerClient, type: CategoryType.Income);

        // Act
        var response = await otherClient.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create.Id), new
        {
            name = UniqueCategoryName("OtherUser"),
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-UPDATE-006: UpdateCategory_WithUnknownId_ReturnsNotFound")]
    public async Task UpdateCategory_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = UniqueCategoryName("Unknown"),
            icon = "🏷️",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-DELETE-001: DeleteCategory_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteCategory_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CATS-DELETE-002: DeleteCategory_WithOwnedUnusedCategory_ReturnsOk")]
    public async Task DeleteCategory_WithOwnedUnusedCategory_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateCategoryAsync(client, type: CategoryType.Expense);

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteCategoryResponse>();
        body!.Success.Should().BeTrue();

        var categoriesResponse = await client.GetAsync(GetCategoriesEndpoint.Route);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories!.Should().NotContain(x => x.Id == create.Id);
    }

    [Fact(DisplayName = "CATS-DELETE-003: DeleteCategory_WithSystemCategory_ReturnsForbidden")]
    public async Task DeleteCategory_WithSystemCategory_ReturnsForbidden()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var systemCategory = await GetSystemCategoryAsync(client, CategoryType.Expense);

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(systemCategory.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CATS-DELETE-004: DeleteCategory_WithOtherUsersCategory_ReturnsNotFound")]
    public async Task DeleteCategory_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var create = await CreateCategoryAsync(ownerClient, type: CategoryType.Expense);

        // Act
        var response = await otherClient.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-DELETE-005: DeleteCategory_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteCategory_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-DELETE-006: DeleteCategory_WithReferencedTransactions_ReturnsBadRequest")]
    public async Task DeleteCategory_WithReferencedTransactions_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client);
        var category = await CreateCategoryAsync(client, type: CategoryType.Expense);
        await CreateTransactionAsync(client, wallet.Id, category.Id, 20m, TransactionType.Expense, "2025-03-01");

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(category.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Category is in use by transactions");
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        return await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
    }

    private static string UniqueCategoryName(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }

    private async Task<CreateCategoryResponse> CreateCategoryAsync(HttpClient client, string? name = null, CategoryType type = CategoryType.Expense, string? icon = "🏷️")
    {
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = name ?? UniqueCategoryName("Category"),
            type = type.ToString(),
            icon,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateCategoryResponse>())!;
    }

    private async Task<CreateWalletResponse> CreateWalletAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance = 100m,
            currency = "USD",
            icon = "💳",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateWalletResponse>())!;
    }

    private async Task<CreateTransactionResponse> CreateTransactionAsync(HttpClient client, Guid walletId, Guid categoryId, decimal amount, TransactionType type, string date)
    {
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId,
            amount,
            type = type.ToString(),
            date,
            note = $"Txn_{Guid.NewGuid():N}",
            categoryId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateTransactionResponse>())!;
    }

    private async Task<CategoryResponse> GetSystemCategoryAsync(HttpClient client, CategoryType type)
    {
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={type}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        return categories!.First(x => x.IsSystem && x.Type == type);
    }
}
