using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Categories;
using SourceBase.Api.Features.Transactions;
using SourceBase.Api.Features.Wallets;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

public class WalletSummaryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-SUMMARY-001: GetWalletSummary_WithoutToken_ReturnsUnauthorized")]
    public async Task GetWalletSummary_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-002: GetWalletSummary_WithWalletBalances_ReturnsCorrectTotalBalance")]
    public async Task GetWalletSummary_WithWalletBalances_ReturnsCorrectTotalBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var firstWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💼" });
        firstWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstWallet = await firstWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var secondWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💼" });
        secondWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondWallet = await secondWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = firstWallet!.Id, amount = 25m, type = "Income", date = "2025-02-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = secondWallet!.Id, amount = 10m, type = "Expense", date = "2025-02-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.TotalBalance.Should().Be(165m);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-003: GetWalletSummary_WithCurrentMonthTransactions_ReturnsMonthlyTotals")]
    public async Task GetWalletSummary_WithCurrentMonthTransactions_ReturnsMonthlyTotals()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💼" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var previousMonth = today.AddMonths(-1);
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 100m, type = "Income", date = today.ToString("yyyy-MM-dd"), note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 40m, type = "Expense", date = today.AddDays(-1).ToString("yyyy-MM-dd"), note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 70m, type = "Income", date = previousMonth.ToString("yyyy-MM-dd"), note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.MonthlyIncome.Should().Be(100m);
        body.MonthlyExpense.Should().Be(40m);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-004: GetWalletSummary_WithMoreThanFiveTransactions_ReturnsAtMostFiveRecentTransactions")]
    public async Task GetWalletSummary_WithMoreThanFiveTransactions_ReturnsAtMostFiveRecentTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💼" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var createdTransactionIds = new List<Guid>();
        for (var day = 1; day <= 6; day++)
        {
            var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m + day, type = "Income", date = $"2025-06-{day:00}", note = $"Recent_{day}", categoryId = incomeCategoryId });
            txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var txn = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
            createdTransactionIds.Add(txn!.Id);
        }

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.RecentTransactions.Should().HaveCount(5);
        body.RecentTransactions.Should().NotContain(x => x.Id == createdTransactionIds[0]);
        body.RecentTransactions.Select(x => x.Id).Should().BeEquivalentTo(createdTransactionIds.Skip(1));
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-005: GetWalletSummary_WithNoWallets_ReturnsZerosAndEmptyRecentTransactions")]
    public async Task GetWalletSummary_WithNoWallets_ReturnsZerosAndEmptyRecentTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.TotalBalance.Should().Be(0m);
        body.MonthlyIncome.Should().Be(0m);
        body.MonthlyExpense.Should().Be(0m);
        body.RecentTransactions.Should().BeEmpty();
    }
}

