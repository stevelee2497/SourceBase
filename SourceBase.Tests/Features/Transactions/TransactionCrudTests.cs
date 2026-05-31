using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Categories;
using SourceBase.Api.Features.Transactions;
using SourceBase.Api.Features.Transfers;
using SourceBase.Api.Features.Wallets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transactions;

public class TransactionCrudTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
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

    [Fact(DisplayName = "TXN-GET-ALL-001: GetTransactions_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTransactions_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransactionsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-GET-ALL-002: GetTransactions_WithOwnedTransactions_ReturnsOk")]
    public async Task GetTransactions_WithOwnedTransactions_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Income", date = "2025-01-10", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().Contain(x => x.Id == transaction!.Id);
        body.Total.Should().Be(1);
    }

    [Fact(DisplayName = "TXN-GET-ALL-003: GetTransactions_WithMultipleUsers_ReturnsOnlyCurrentUsersTransactions")]
    public async Task GetTransactions_WithMultipleUsers_ReturnsOnlyCurrentUsersTransactions()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownerWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var ownerWallet = await ownerWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var otherWalletResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var otherWallet = await otherWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var ownTxnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = ownerWallet!.Id, amount = 20m, type = "Income", date = "2025-01-11", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var ownTransaction = await ownTxnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        var otherIncomeCatResponse = await otherClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var otherIncomeCategories = await otherIncomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var otherIncomeCategoryId = otherIncomeCategories!.First(x => x.IsSystem).Id;
        await otherClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = otherWallet!.Id, amount = 15m, type = "Income", date = "2025-01-11", note = $"Txn_{Guid.NewGuid():N}", categoryId = otherIncomeCategoryId });

        // Act
        var response = await ownerClient.GetAsync($"{GetTransactionsEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == ownTransaction!.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-004: GetTransactions_WithWalletFilter_ReturnsMatchingTransactions")]
    public async Task GetTransactions_WithWalletFilter_ReturnsMatchingTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletAResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var walletA = await walletAResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletBResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var walletB = await walletBResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnAResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletA!.Id, amount = 10m, type = "Income", date = "2025-01-12", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var transactionA = await txnAResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletB!.Id, amount = 20m, type = "Income", date = "2025-01-12", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={walletA.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == transactionA!.Id && x.WalletId == walletA.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-005: GetTransactions_WithIncomeTypeFilter_ReturnsOnlyIncomeTransactions")]
    public async Task GetTransactions_WithIncomeTypeFilter_ReturnsOnlyIncomeTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-13", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 5m, type = "Expense", date = "2025-01-13", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?type=Income&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().OnlyContain(x => x.Type == TransactionType.Income);
    }

    [Fact(DisplayName = "TXN-GET-ALL-006: GetTransactions_WithExpenseTypeFilter_ReturnsOnlyExpenseTransactions")]
    public async Task GetTransactions_WithExpenseTypeFilter_ReturnsOnlyExpenseTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-14", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 5m, type = "Expense", date = "2025-01-14", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?type=Expense&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().OnlyContain(x => x.Type == TransactionType.Expense);
    }

    [Fact(DisplayName = "TXN-GET-ALL-007: GetTransactions_WithDateRangeFilter_ReturnsTransactionsWithinRange")]
    public async Task GetTransactions_WithDateRangeFilter_ReturnsTransactionsWithinRange()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var inRangeResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 15m, type = "Income", date = "2025-01-15", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var inRange = await inRangeResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 20m, type = "Income", date = "2025-02-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?dateFrom=2025-01-10&dateTo=2025-01-20&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == inRange!.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-008: GetTransactions_WithCategoryFilter_ReturnsMatchingTransactions")]
    public async Task GetTransactions_WithCategoryFilter_ReturnsMatchingTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var firstCatResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        firstCatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstCategory = await firstCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var secondCatResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        secondCatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondCategory = await secondCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var matchingTxnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Expense", date = "2025-01-16", note = $"Txn_{Guid.NewGuid():N}", categoryId = firstCategory!.Id });
        matchingTxnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var matching = await matchingTxnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 5m, type = "Expense", date = "2025-01-16", note = $"Txn_{Guid.NewGuid():N}", categoryId = secondCategory!.Id });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?categoryId={firstCategory.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == matching!.Id && x.CategoryId == firstCategory.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-009: GetTransactions_WithPagination_ReturnsCorrectSubset")]
    public async Task GetTransactions_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var secondTxnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 20m, type = "Income", date = "2025-01-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var second = await secondTxnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 30m, type = "Income", date = "2025-01-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?page=2&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Total.Should().Be(3);
        body.Items.Should().ContainSingle();
        body.Items[0].Id.Should().Be(second!.Id);
    }

    [Fact(DisplayName = "TXN-GET-001: GetTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-GET-002: GetTransaction_WithOwnedTransaction_ReturnsTransactionData")]
    public async Task GetTransaction_WithOwnedTransaction_ReturnsTransactionData()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 40m, type = "Income", date = "2025-01-20", note = "Salary payment", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(created!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.Id.Should().Be(created.Id);
        body.WalletId.Should().Be(wallet.Id);
        body.WalletName.Should().StartWith("Wallet_");
        body.CategoryId.Should().Be(category.Id);
        body.CategoryName.Should().Be(category.Name);
        body.Type.Should().Be(TransactionType.Income);
        body.Note.Should().Be("Salary payment");
        body.IsTransfer.Should().BeFalse();
    }

    [Fact(DisplayName = "TXN-GET-003: GetTransaction_WithUnknownId_ReturnsNotFound")]
    public async Task GetTransaction_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-GET-004: GetTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task GetTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-21", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await otherClient.GetAsync(GetTransactionEndpoint.Route.WithId(transaction!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-UPDATE-001: UpdateTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            amount = 50m,
            type = "Income",
            date = "2025-01-22",
            categoryId = Guid.NewGuid(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-UPDATE-002: UpdateTransaction_WithValidData_ReturnsOk")]
    public async Task UpdateTransaction_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var originalCategory = incomeCategories!.First(x => x.IsSystem);

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var updatedCategory = expenseCategories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Income", date = "2025-01-23", note = "Original note", categoryId = originalCategory.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 55m,
            type = "Expense",
            date = "2025-02-02",
            note = "Updated note",
            categoryId = updatedCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateTransactionResponse>();
        body!.Id.Should().Be(transaction.Id);

        var transactionResponse = await client.GetAsync(GetTransactionEndpoint.Route.WithId(transaction.Id));
        var updated = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updated!.Amount.Should().Be(55m);
        updated.Type.Should().Be(TransactionType.Expense);
        updated.Date.Should().Be(new DateOnly(2025, 2, 2));
        updated.Note.Should().Be("Updated note");
        updated.CategoryId.Should().Be(updatedCategory.Id);
    }

    [Fact(DisplayName = "TXN-UPDATE-003: UpdateTransaction_WhenAmountChanges_RecomputesWalletBalance")]
    public async Task UpdateTransaction_WhenAmountChanges_RecomputesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-24", note = $"Txn_{Guid.NewGuid():N}", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 40m,
            type = "Income",
            date = "2025-01-24",
            note = "Adjusted",
            categoryId = category.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(140m);
    }

    [Fact(DisplayName = "TXN-UPDATE-004: UpdateTransaction_WhenTypeChanges_RecomputesWalletBalance")]
    public async Task UpdateTransaction_WhenTypeChanges_RecomputesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategory = incomeCategories!.First(x => x.IsSystem);

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategory = expenseCategories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 30m, type = "Income", date = "2025-01-25", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategory.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 30m,
            type = "Expense",
            date = "2025-01-25",
            note = "Converted to expense",
            categoryId = expenseCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(70m);
    }

    [Fact(DisplayName = "TXN-UPDATE-005: UpdateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 30m, type = "Income", date = "2025-01-26", note = $"Txn_{Guid.NewGuid():N}", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var zeroResponse = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 0m,
            type = "Income",
            date = "2025-01-26",
            note = "Zero",
            categoryId = category.Id,
        });
        var negativeResponse = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = -5m,
            type = "Income",
            date = "2025-01-26",
            note = "Negative",
            categoryId = category.Id,
        });

        // Assert
        zeroResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        negativeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-UPDATE-006: UpdateTransaction_WithUnknownId_ReturnsNotFound")]
    public async Task UpdateTransaction_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            amount = 30m,
            type = "Income",
            date = "2025-01-27",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-UPDATE-007: UpdateTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task UpdateTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Income", date = "2025-01-28", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        var otherCatResponse = await otherClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var otherCategories = await otherCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var otherIncomeCategoryId = otherCategories!.First(x => x.IsSystem).Id;

        // Act
        var response = await otherClient.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 50m,
            type = "Income",
            date = "2025-01-28",
            categoryId = otherIncomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-UPDATE-008: UpdateTransaction_WithTransferTransaction_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithTransferTransaction_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 30m, date = "2025-01-29", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        var transferTransactionId = await factory.WithDbContextAsync(async db => await db.Transfers.Where(x => x.Id == transfer!.Id).Select(x => x.FromTransactionId).SingleAsync());

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transferTransactionId), new
        {
            amount = 35m,
            type = "Expense",
            date = "2025-01-29",
            note = "Updated transfer",
            categoryId = expenseCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Transfer transactions cannot be edited directly");
    }

    [Fact(DisplayName = "TXN-DELETE-001: DeleteTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-DELETE-002: DeleteTransaction_WithIncomeTransaction_RecomputesWalletBalance")]
    public async Task DeleteTransaction_WithIncomeTransaction_RecomputesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 25m, type = "Income", date = "2025-01-30", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transaction!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(100m);
    }

    [Fact(DisplayName = "TXN-DELETE-003: DeleteTransaction_WithExpenseTransaction_RecomputesWalletBalance")]
    public async Task DeleteTransaction_WithExpenseTransaction_RecomputesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 25m, type = "Expense", date = "2025-01-31", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transaction!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(100m);
    }

    [Fact(DisplayName = "TXN-DELETE-004: DeleteTransaction_WithTransferTransaction_ReturnsBadRequest")]
    public async Task DeleteTransaction_WithTransferTransaction_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 30m, date = "2025-02-01", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        var transferTransactionId = await factory.WithDbContextAsync(async db => await db.Transfers.Where(x => x.Id == transfer!.Id).Select(x => x.FromTransactionId).SingleAsync());

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transferTransactionId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Transfer transactions cannot be deleted directly");
    }

    [Fact(DisplayName = "TXN-DELETE-005: DeleteTransaction_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteTransaction_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-DELETE-006: DeleteTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task DeleteTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 25m, type = "Income", date = "2025-02-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transaction!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
