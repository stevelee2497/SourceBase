using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transactions;

public class CreateTransactionTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TXN-CREATE-001: CreateTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = Guid.NewGuid(),
            amount = 100m,
            type = "Income",
            date = "2025-01-01",
            categoryId = Guid.NewGuid(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-CREATE-002: CreateTransaction_WithIncome_UpdatesWalletBalance")]
    public async Task CreateTransaction_WithIncome_UpdatesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            amount = 25m,
            type = "Income",
            date = "2025-01-01",
            note = $"Income_{Guid.NewGuid():N}",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        body!.Id.Should().NotBeEmpty();

        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(125m);
    }

    [Fact(DisplayName = "TXN-CREATE-003: CreateTransaction_WithExpense_UpdatesWalletBalance")]
    public async Task CreateTransaction_WithExpense_UpdatesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            amount = 30m,
            type = "Expense",
            date = "2025-01-02",
            note = $"Expense_{Guid.NewGuid():N}",
            categoryId = expenseCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        body!.Id.Should().NotBeEmpty();

        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(70m);
    }

    [Fact(DisplayName = "TXN-CREATE-004: CreateTransaction_WithMissingWalletId_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingWalletId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            amount = 20m,
            type = "Income",
            date = "2025-01-03",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-005: CreateTransaction_WithMissingAmount_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            type = "Income",
            date = "2025-01-04",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-006: CreateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest")]
    public async Task CreateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var zeroResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            amount = 0m,
            type = "Income",
            date = "2025-01-05",
            categoryId = incomeCategoryId,
        });
        var negativeResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = -1m,
            type = "Expense",
            date = "2025-01-05",
            categoryId = incomeCategoryId,
        });

        // Assert
        zeroResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        negativeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-007: CreateTransaction_WithMissingDate_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingDate_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            amount = 20m,
            type = "Income",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-008: CreateTransaction_WithMissingType_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingType_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            amount = 20m,
            date = "2025-01-06",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-009: CreateTransaction_WithOtherUsersWallet_ReturnsNotFound")]
    public async Task CreateTransaction_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherWallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = otherWallet!.Id,
            amount = 20m,
            type = "Income",
            date = "2025-01-07",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-CREATE-010: CreateTransaction_WithOtherUsersCategory_ReturnsNotFound")]
    public async Task CreateTransaction_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var otherCatResponse = await otherClient.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        otherCatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherCategory = await otherCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            amount = 20m,
            type = "Expense",
            date = "2025-01-08",
            categoryId = otherCategory!.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-CREATE-011: CreateTransaction_WithoutCategory_ReturnsBadRequest")]
    public async Task CreateTransaction_WithoutCategory_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet!.Id,
            amount = 45m,
            type = "Income",
            date = "2025-01-09",
            note = $"NoCategory_{Guid.NewGuid():N}",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
