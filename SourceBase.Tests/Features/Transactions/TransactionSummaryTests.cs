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
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, wallet.Id, 100m, TransactionType.Income, "2025-03-01");
        await CreateTransactionAsync(client, wallet.Id, 40m, TransactionType.Expense, "2025-03-02");

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
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, wallet.Id, 100m, TransactionType.Income, "2025-03-03");
        await CreateTransactionAsync(client, wallet.Id, 40m, TransactionType.Expense, "2025-03-04");

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
        var client = await CreateUserClientAsync();
        var firstWallet = await CreateWalletAsync(client, 0m);
        var secondWallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, firstWallet.Id, 100m, TransactionType.Income, "2025-03-05");
        await CreateTransactionAsync(client, secondWallet.Id, 40m, TransactionType.Expense, "2025-03-05");

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
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, wallet.Id, 10m, TransactionType.Income, "2025-02-28");
        await CreateTransactionAsync(client, wallet.Id, 30m, TransactionType.Income, "2025-03-10");
        await CreateTransactionAsync(client, wallet.Id, 20m, TransactionType.Expense, "2025-04-01");

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
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var salaryCategory = await GetSystemCategoryAsync(client, CategoryType.Income);
        var foodCategory = await CreateCustomCategoryAsync(client, CategoryType.Expense);
        await CreateTransactionAsync(client, wallet.Id, 120m, TransactionType.Income, "2025-03-11", salaryCategory.Id);
        await CreateTransactionAsync(client, wallet.Id, 35m, TransactionType.Expense, "2025-03-12", foodCategory.Id);
        await CreateTransactionAsync(client, wallet.Id, 15m, TransactionType.Expense, "2025-03-13", foodCategory.Id);

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
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var ownerWallet = await CreateWalletAsync(ownerClient, 0m);
        var otherWallet = await CreateWalletAsync(otherClient, 0m);
        await CreateTransactionAsync(ownerClient, ownerWallet.Id, 75m, TransactionType.Income, "2025-03-14");
        await CreateTransactionAsync(otherClient, otherWallet.Id, 200m, TransactionType.Income, "2025-03-14");

        // Act
        var response = await ownerClient.GetAsync($"{GetTransactionSummaryEndpoint.Route}?dateFrom=2025-03-01&dateTo=2025-03-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTransactionSummaryResponse>();
        body!.TotalIncome.Should().Be(75m);
        body.TotalExpense.Should().Be(0m);
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        return await factory.CreateAuthorizedClient($"transaction_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");
    }

    private async Task<CreateWalletResponse> CreateWalletAsync(HttpClient client, decimal initialBalance)
    {
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance,
            currency = "USD",
            icon = "💳",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateWalletResponse>())!;
    }

    private async Task<CreateCategoryResponse> CreateCustomCategoryAsync(HttpClient client, CategoryType type)
    {
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = $"Category_{Guid.NewGuid():N}",
            type = type.ToString(),
            icon = "🏷️",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateCategoryResponse>())!;
    }

    private async Task<CategoryResponse> GetSystemCategoryAsync(HttpClient client, CategoryType type)
    {
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={type}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        return categories!.First(x => x.IsSystem && x.Type == type);
    }

    private async Task<CreateTransactionResponse> CreateTransactionAsync(HttpClient client, Guid walletId, decimal amount, TransactionType type, string date, Guid? categoryId = null)
    {
        var effectiveCategoryId = categoryId;
        if (effectiveCategoryId is null)
        {
            var systemCategory = await GetSystemCategoryAsync(client, type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense);
            effectiveCategoryId = systemCategory.Id;
        }

        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId,
            amount,
            type = type.ToString(),
            date,
            note = $"Txn_{Guid.NewGuid():N}",
            categoryId = effectiveCategoryId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateTransactionResponse>())!;
    }
}
