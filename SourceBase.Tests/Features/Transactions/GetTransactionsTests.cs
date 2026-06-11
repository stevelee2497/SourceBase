using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transactions;

public class GetTransactionsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
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

    [Fact(DisplayName = "TXN-GET-ALL-010: GetTransactions_WithSingleWalletIdsFilter_ReturnsMatchingTransactions")]
    public async Task GetTransactions_WithSingleWalletIdsFilter_ReturnsMatchingTransactions()
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

        var txnAResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletA!.Id, amount = 10m, type = "Income", date = "2025-02-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var transactionA = await txnAResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletB!.Id, amount = 20m, type = "Income", date = "2025-02-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletIds={walletA.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == transactionA!.Id && x.WalletId == walletA.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-011: GetTransactions_WithMultipleWalletIdsFilter_ReturnsTransactionsFromAllSpecifiedWallets")]
    public async Task GetTransactions_WithMultipleWalletIdsFilter_ReturnsTransactionsFromAllSpecifiedWallets()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletAResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var walletA = await walletAResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletBResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var walletB = await walletBResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletCResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var walletC = await walletCResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnAResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletA!.Id, amount = 10m, type = "Income", date = "2025-02-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var transactionA = await txnAResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        var txnBResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletB!.Id, amount = 20m, type = "Income", date = "2025-02-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var transactionB = await txnBResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletC!.Id, amount = 30m, type = "Income", date = "2025-02-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletIds={walletA.Id}&walletIds={walletB.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Total.Should().Be(2);
        body.Items.Should().Contain(x => x.Id == transactionA!.Id && x.WalletId == walletA.Id);
        body.Items.Should().Contain(x => x.Id == transactionB!.Id && x.WalletId == walletB.Id);
        body.Items.Should().NotContain(x => x.WalletId == walletC!.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-012: GetTransactions_WithWalletIdsFilterCombinedWithTypeFilter_ReturnsMatchingTransactions")]
    public async Task GetTransactions_WithWalletIdsFilterCombinedWithTypeFilter_ReturnsMatchingTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletAResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var walletA = await walletAResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletBResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var walletB = await walletBResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        var incomeTxnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletA!.Id, amount = 10m, type = "Income", date = "2025-02-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        var incomeTxn = await incomeTxnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletA.Id, amount = 5m, type = "Expense", date = "2025-02-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletB!.Id, amount = 15m, type = "Income", date = "2025-02-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act — filter walletA only with Income type
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletIds={walletA.Id}&type=Income&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == incomeTxn!.Id);
        body.Items.Should().OnlyContain(x => x.WalletId == walletA.Id && x.Type == TransactionType.Income);
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
}
