using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
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
        var client = await CreateUserClientAsync();
        var firstWallet = await CreateWalletAsync(client, 100m);
        var secondWallet = await CreateWalletAsync(client, 50m);
        await CreateTransactionAsync(client, firstWallet.Id, 25m, TransactionType.Income, "2025-02-01");
        await CreateTransactionAsync(client, secondWallet.Id, 10m, TransactionType.Expense, "2025-02-02");

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
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var previousMonth = today.AddMonths(-1);
        await CreateTransactionAsync(client, wallet.Id, 100m, TransactionType.Income, today.ToString("yyyy-MM-dd"));
        await CreateTransactionAsync(client, wallet.Id, 40m, TransactionType.Expense, today.AddDays(-1).ToString("yyyy-MM-dd"));
        await CreateTransactionAsync(client, wallet.Id, 70m, TransactionType.Income, previousMonth.ToString("yyyy-MM-dd"));

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
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var createdTransactionIds = new List<Guid>();

        for (var day = 1; day <= 6; day++)
        {
            var transaction = await CreateTransactionAsync(client, wallet.Id, 10m + day, TransactionType.Income, $"2025-06-{day:00}", $"Recent_{day}");
            createdTransactionIds.Add(transaction.Id);
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
        var client = await CreateUserClientAsync();

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

    private async Task<HttpClient> CreateUserClientAsync()
    {
        return await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");
    }

    private async Task<CreateWalletResponse> CreateWalletAsync(HttpClient client, decimal initialBalance)
    {
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance,
            currency = "USD",
            icon = "💼",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateWalletResponse>())!;
    }

    private async Task<CreateTransactionResponse> CreateTransactionAsync(HttpClient client, Guid walletId, decimal amount, TransactionType type, string date, string? note = null)
    {
        var category = await GetSystemCategoryAsync(client, type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense);
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId,
            amount,
            type = type.ToString(),
            date,
            note = note ?? $"Txn_{Guid.NewGuid():N}",
            categoryId = category.Id,
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
