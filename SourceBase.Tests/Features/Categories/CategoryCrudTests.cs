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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>();
        body!.Id.Should().NotBeEmpty();
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CATS-CREATE-006: CreateCategory_WithAuthenticatedUser_SetsOwnershipAndNonSystem")]
    public async Task CreateCategory_WithAuthenticatedUser_SetsOwnershipAndNonSystem()
    {
        // Arrange
        var email = $"category_owner_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");

        // Act
        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Income", icon = "🏷️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Assert
        var data = await factory.WithDbContextAsync(async db => new
        {
            Category = await db.Categories.SingleAsync(x => x.Id == create!.Id),
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
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
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
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            icon = "��️",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(systemCategory.Id), new
        {
            name = $"System_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        var response = await otherClient.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(create!.Id), new
        {
            name = $"OtherUser_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-UPDATE-006: UpdateCategory_WithUnknownId_ReturnsNotFound")]
    public async Task UpdateCategory_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PutAsJsonAsync(UpdateCategoryEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Unknown_{Guid.NewGuid():N}",
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
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(create!.Id));

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
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var catListResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        catListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await catListResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var systemCategory = categories!.First(x => x.IsSystem);

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(systemCategory.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CATS-DELETE-004: DeleteCategory_WithOtherUsersCategory_ReturnsNotFound")]
    public async Task DeleteCategory_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-DELETE-005: DeleteCategory_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteCategory_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "CATS-DELETE-006: DeleteCategory_WithReferencedTransactions_ReturnsBadRequest")]
    public async Task DeleteCategory_WithReferencedTransactions_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"category_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        catResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await catResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Expense", date = "2025-03-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = category!.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.DeleteAsync(DeleteCategoryEndpoint.Route.WithId(category.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Category is in use by transactions");
    }
}
