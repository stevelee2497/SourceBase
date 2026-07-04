using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transactions;

[EndpointFact(
    Feature = "Transactions",
    Name = "Get Transactions",
    Route = "GET /api/transactions",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to list my transactions with filtering and pagination, so that I can browse and analyse my transaction history.",
    Description = new[]
    {
        "Client sends optional filters: `walletId`, `type`, `categoryId`, `dateFrom`, `dateTo`, plus paging parameters (`page`, `pageSize`).",
        "Returns only transactions belonging to the authenticated user.",
        "Results are ordered by `date` descending, then by `CreatedOn` descending.",
        "Each item includes wallet name and category name for display.",
    })]
public class GetTransactionsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TXN-GET-ALL-001: missing token returns 401")]
    public async Task GetTransactions_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransactionsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-GET-ALL-002: owned transactions return 200")]
    public async Task GetTransactions_WithOwnedTransactions_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Income", date = "2025-01-10", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldContain(x => x.Id == transaction!.Id);
        body.Total.ShouldBe(1);
    }

    [Fact(DisplayName = "TXN-GET-ALL-003: multiple users returns only current user's transactions")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldContain(x => x.Id == ownTransaction!.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-004: wallet filter returns matching transactions")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldContain(x => x.Id == transactionA!.Id && x.WalletId == walletA.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-005: income type filter returns only income transactions")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldNotBeEmpty();
        body.Items.ShouldAllBe(x => x.Type == TransactionType.Income);
    }

    [Fact(DisplayName = "TXN-GET-ALL-006: expense type filter returns only expense transactions")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldNotBeEmpty();
        body.Items.ShouldAllBe(x => x.Type == TransactionType.Expense);
    }

    [Fact(DisplayName = "TXN-GET-ALL-007: date range filter returns transactions within range")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldContain(x => x.Id == inRange!.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-008: category filter returns matching transactions")]
    public async Task GetTransactions_WithCategoryFilter_ReturnsMatchingTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var firstCatResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        firstCatResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstCategory = await firstCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var secondCatResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Category_{Guid.NewGuid():N}", type = "Expense", icon = "🏷️" });
        secondCatResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondCategory = await secondCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var matchingTxnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Expense", date = "2025-01-16", note = $"Txn_{Guid.NewGuid():N}", categoryId = firstCategory!.Id });
        matchingTxnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var matching = await matchingTxnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 5m, type = "Expense", date = "2025-01-16", note = $"Txn_{Guid.NewGuid():N}", categoryId = secondCategory!.Id });

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?categoryId={firstCategory.Id}&limit=20");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldContain(x => x.Id == matching!.Id && x.CategoryId == firstCategory.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-010: single wallet ids filter returns matching transactions")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldContain(x => x.Id == transactionA!.Id && x.WalletId == walletA.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-011: multiple wallet ids filter returns transactions from all specified wallets")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Total.ShouldBe(2);
        body.Items.ShouldContain(x => x.Id == transactionA!.Id && x.WalletId == walletA.Id);
        body.Items.ShouldContain(x => x.Id == transactionB!.Id && x.WalletId == walletB.Id);
        body.Items.ShouldNotContain(x => x.WalletId == walletC!.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-012: wallet ids filter combined with type filter returns matching transactions")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.ShouldContain(x => x.Id == incomeTxn!.Id);
        body.Items.ShouldAllBe(x => x.WalletId == walletA.Id && x.Type == TransactionType.Income);
    }

    [Fact(DisplayName = "TXN-GET-ALL-009: pagination returns correct subset")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Total.ShouldBe(3);
        body.Items.Count.ShouldBe(1);
        body.Items[0].Id.ShouldBe(second!.Id);
    }
}
