using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Tests.Infrastructure;
using Xunit;
using Xunit.Sdk;

namespace SourceBase.Tests.Features.Wallets;

public class WalletSummaryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    private static readonly bool UseRedis = string.Equals(Environment.GetEnvironmentVariable("USE_REDIS"), "true", StringComparison.OrdinalIgnoreCase);
    [Fact(DisplayName = "WALLETS-SUMMARY-001: GetWalletSummary_WithoutToken_ReturnsUnauthorized")]
    public async Task GetWalletSummary_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-002: GetWalletSummary_WithWalletBalances_ReturnsCorrectTotalBalance")]
    public async Task GetWalletSummary_WithWalletBalances_ReturnsCorrectTotalBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var firstWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💼" });
        firstWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstWallet = await firstWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var secondWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💼" });
        secondWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.TotalBalance.ShouldBe(165m);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-003: GetWalletSummary_WithCurrentMonthTransactions_ReturnsMonthlyTotals")]
    public async Task GetWalletSummary_WithCurrentMonthTransactions_ReturnsMonthlyTotals()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💼" });
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 100m, type = "Income", date = "2026-06-04", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 40m, type = "Expense", date = "2026-06-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet.Id, amount = 70m, type = "Income", date = "2026-05-04", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.MonthlyIncome.ShouldBe(100m);
        body.MonthlyExpense.ShouldBe(40m);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-004: GetWalletSummary_WithMoreThanFiveTransactions_ReturnsAtMostFiveRecentTransactions")]
    public async Task GetWalletSummary_WithMoreThanFiveTransactions_ReturnsAtMostFiveRecentTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💼" });
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var createdTransactionIds = new List<Guid>();
        for (var day = 1; day <= 6; day++)
        {
            var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m + day, type = "Income", date = $"2025-06-{day:00}", note = $"Recent_{day}", categoryId = incomeCategoryId });
            txnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            var txn = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();
            createdTransactionIds.Add(txn!.Id);
        }

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.RecentTransactions.Count.ShouldBe(5);
        body.RecentTransactions.ShouldNotContain(x => x.Id == createdTransactionIds[0]);
        body.RecentTransactions.Select(x => x.Id).ShouldBe(createdTransactionIds.Skip(1), ignoreOrder: true);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-005: GetWalletSummary_WithNoWallets_ReturnsZerosAndEmptyRecentTransactions")]
    public async Task GetWalletSummary_WithNoWallets_ReturnsZerosAndEmptyRecentTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        body!.TotalBalance.ShouldBe(0m);
        body.MonthlyIncome.ShouldBe(0m);
        body.MonthlyExpense.ShouldBe(0m);
        body.RecentTransactions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-006: GetWalletSummary_CachesResult_ServesStaleDataBeforeCacheIsInvalidated")]
    public async Task GetWalletSummary_CachesResult_ServesStaleDataBeforeCacheIsInvalidated()
    {
        if (!UseRedis) throw SkipException.ForSkip("Requires Redis test container (USE_REDIS=true)");

        // Arrange — fresh user so wallet-summary:{userId} key is isolated
        var client = await factory.CreateAuthorizedClient($"ws_cache_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD" });
        var wallet = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Warm the cache — first GET populates wallet-summary:{userId}
        var firstResponse = await client.GetAsync(GetWalletSummaryEndpoint.Route);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        firstBody!.TotalBalance.ShouldBe(100m);

        // Bypass the API and change InitialBalance directly in DB (no cache invalidation triggered)
        await factory.WithDbContextAsync(async db =>
        {
            var entity = await db.Wallets.FindAsync(wallet!.Id);
            entity!.InitialBalance = 999m;
            await db.SaveChangesAsync();
            return true;
        });

        // Act — second GET should still return the cached (stale) value
        var secondResponse = await client.GetAsync(GetWalletSummaryEndpoint.Route);

        // Assert — Redis served the old cached balance; the direct DB change is invisible
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        secondBody!.TotalBalance.ShouldBe(100m);
        secondBody.TotalBalance.ShouldNotBe(999m);
    }

    [Fact(DisplayName = "WALLETS-SUMMARY-007: GetWalletSummary_AfterCreateWallet_CacheIsInvalidatedAndReturnsFreshBalance")]
    public async Task GetWalletSummary_AfterCreateWallet_CacheIsInvalidatedAndReturnsFreshBalance()
    {
        // Arrange — fresh user so wallet-summary:{userId} key is isolated
        var client = await factory.CreateAuthorizedClient($"ws_inv_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var firstWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD" });
        firstWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Populate the cache
        var cachedSummaryResponse = await client.GetAsync(GetWalletSummaryEndpoint.Route);
        var cachedSummary = await cachedSummaryResponse.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        cachedSummary!.TotalBalance.ShouldBe(100m);

        // Act — create a second wallet; this should invalidate wallet-summary:{userId}
        var secondWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD" });
        secondWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert — GET summary re-fetches from DB and reflects the new wallet
        var freshResponse = await client.GetAsync(GetWalletSummaryEndpoint.Route);
        freshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var freshBody = await freshResponse.Content.ReadFromJsonAsync<GetWalletSummaryResponse>();
        freshBody!.TotalBalance.ShouldBe(150m);
    }
}

