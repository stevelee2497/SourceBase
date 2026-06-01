using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Categories;
using SourceBase.Api.Features.Transactions;
using SourceBase.Api.Features.Wallets;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transactions;

public class TransactionSummaryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TXN-SUMMARY-001: GetTransactionSummary_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTransactionSummary_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransactionSummaryEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-SUMMARY-002: GetTransactionSummary_WithPeriod_ReturnsTotalIncomeAndExpense")]
    public async Task GetTransactionSummary_WithPeriod_ReturnsTotalIncomeAndExpense()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        var incomeResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 100m, type = "Income", date = "2025-03-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        incomeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var expenseResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 40m, type = "Expense", date = "2025-03-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        expenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"{GetTransactionSummaryEndpoint.Route}?dateFrom=2025-03-01&dateTo=2025-03-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTransactionSummaryResponse>();
        body!.TotalIncome.Should().Be(100m);
        body.TotalExpense.Should().Be(40m);
    }

    [Fact(DisplayName = "TXN-SUMMARY-003: GetTransactionSummary_WithTotals_ReturnsNetBalance")]
    public async Task GetTransactionSummary_WithTotals_ReturnsNetBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        var incomeResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 100m, type = "Income", date = "2025-03-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        incomeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var expenseResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 40m, type = "Expense", date = "2025-03-04", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        expenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"{GetTransactionSummaryEndpoint.Route}?dateFrom=2025-03-01&dateTo=2025-03-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTransactionSummaryResponse>();
        body!.NetBalance.Should().Be(60m);
    }

    [Fact(DisplayName = "TXN-SUMMARY-004: GetTransactionSummary_WithWalletFilter_ReturnsWalletTotalsOnly")]
    public async Task GetTransactionSummary_WithWalletFilter_ReturnsWalletTotalsOnly()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var firstWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        firstWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstWallet = await firstWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var secondWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        secondWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondWallet = await secondWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        var firstTxnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = firstWallet!.Id, amount = 100m, type = "Income", date = "2025-03-05", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        firstTxnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondTxnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = secondWallet!.Id, amount = 40m, type = "Expense", date = "2025-03-05", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        secondTxnResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"{GetTransactionSummaryEndpoint.Route}?walletId={firstWallet.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTransactionSummaryResponse>();
        body!.TotalIncome.Should().Be(100m);
        body.TotalExpense.Should().Be(0m);
        body.NetBalance.Should().Be(100m);
    }

    [Fact(DisplayName = "TXN-SUMMARY-005: GetTransactionSummary_WithDateRange_ReturnsTransactionsWithinRange")]
    public async Task GetTransactionSummary_WithDateRange_ReturnsTransactionsWithinRange()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        var beforeResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-02-28", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inRangeResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 30m, type = "Income", date = "2025-03-10", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        inRangeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 20m, type = "Expense", date = "2025-04-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        afterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"{GetTransactionSummaryEndpoint.Route}?dateFrom=2025-03-01&dateTo=2025-03-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTransactionSummaryResponse>();
        body!.TotalIncome.Should().Be(30m);
        body.TotalExpense.Should().Be(0m);
    }

    [Fact(DisplayName = "TXN-SUMMARY-006: GetTransactionSummary_ByCategory_ReturnsGroupedTotals")]
    public async Task GetTransactionSummary_ByCategory_ReturnsGroupedTotals()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var salaryCategory = incomeCategories!.First(x => x.IsSystem);

        var customCatResponse = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new { name = $"Food_{Guid.NewGuid():N}", type = "Expense", icon = "🍔" });
        customCatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var foodCategory = await customCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var txn1Response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 120m, type = "Income", date = "2025-03-11", note = $"Txn_{Guid.NewGuid():N}", categoryId = salaryCategory.Id });
        txn1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var txn2Response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 35m, type = "Expense", date = "2025-03-12", note = $"Txn_{Guid.NewGuid():N}", categoryId = foodCategory!.Id });
        txn2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var txn3Response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 15m, type = "Expense", date = "2025-03-13", note = $"Txn_{Guid.NewGuid():N}", categoryId = foodCategory.Id });
        txn3Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"{GetTransactionSummaryEndpoint.Route}?dateFrom=2025-03-01&dateTo=2025-03-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTransactionSummaryResponse>();
        body!.ByCategory.Should().Contain(x => x.CategoryId == salaryCategory.Id && x.Total == 120m && x.Type == TransactionType.Income);
        body.ByCategory.Should().Contain(x => x.CategoryId == foodCategory.Id && x.Total == 50m && x.Type == TransactionType.Expense);
    }

    [Fact(DisplayName = "TXN-SUMMARY-007: GetTransactionSummary_WithMultipleUsers_ExcludesOtherUsersTransactions")]
    public async Task GetTransactionSummary_WithMultipleUsers_ExcludesOtherUsersTransactions()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownerWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        ownerWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownerWallet = await ownerWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var otherWalletResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        otherWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherWallet = await otherWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var ownerCatResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var ownerCategories = await ownerCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var ownerIncomeCategoryId = ownerCategories!.First(x => x.IsSystem).Id;

        var otherCatResponse = await otherClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var otherCategories = await otherCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var otherIncomeCategoryId = otherCategories!.First(x => x.IsSystem).Id;

        var ownerTxnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = ownerWallet!.Id, amount = 75m, type = "Income", date = "2025-03-14", note = $"Txn_{Guid.NewGuid():N}", categoryId = ownerIncomeCategoryId });
        ownerTxnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherTxnResponse = await otherClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = otherWallet!.Id, amount = 200m, type = "Income", date = "2025-03-14", note = $"Txn_{Guid.NewGuid():N}", categoryId = otherIncomeCategoryId });
        otherTxnResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await ownerClient.GetAsync($"{GetTransactionSummaryEndpoint.Route}?dateFrom=2025-03-01&dateTo=2025-03-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTransactionSummaryResponse>();
        body!.TotalIncome.Should().Be(75m);
        body.TotalExpense.Should().Be(0m);
    }
}
