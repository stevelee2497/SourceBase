using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

public class GetWalletsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-GET-ALL-001: GetWallets_WithoutToken_ReturnsUnauthorized")]
    public async Task GetWallets_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-002: GetWallets_WithOwnedWallets_ReturnsOk")]
    public async Task GetWallets_WithOwnedWallets_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var firstResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Cash_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await firstResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var secondResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Bank_{Guid.NewGuid():N}", initialBalance = 75m, currency = "USD", icon = "🏦" });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await secondResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.Should().Contain(x => x.Id == first!.Id);
        body.Wallets.Should().Contain(x => x.Id == second!.Id);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-003: GetWallets_WithMultipleUsers_ReturnsOnlyCurrentUsersWallets")]
    public async Task GetWallets_WithMultipleUsers_ReturnsOnlyCurrentUsersWallets()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Own_{Guid.NewGuid():N}", initialBalance = 10m, currency = "USD", icon = "💳" });
        ownWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownWallet = await ownWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Other_{Guid.NewGuid():N}", initialBalance = 20m, currency = "USD", icon = "💳" });

        // Act
        var response = await ownerClient.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.Should().ContainSingle(x => x.Id == ownWallet!.Id);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-004: GetWallets_WithBalances_ReturnsCorrectTotalBalance")]
    public async Task GetWallets_WithBalances_ReturnsCorrectTotalBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var firstWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Main_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        firstWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstWallet = await firstWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var secondWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Savings_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        secondWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondWallet = await secondWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = firstWallet!.Id, amount = 25m, type = "Income", date = "2025-01-10", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = secondWallet!.Id, amount = 10m, type = "Expense", date = "2025-01-11", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.TotalBalance.Should().Be(165m);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-005: GetWallets_WithNoWallets_ReturnsEmptyList")]
    public async Task GetWallets_WithNoWallets_ReturnsEmptyList()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.Should().BeEmpty();
        body.TotalBalance.Should().Be(0m);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-006: GetWallets_ReturnsWalletCurrencyAndIcon")]
    public async Task GetWallets_ReturnsWalletCurrencyAndIcon()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "EUR", icon = "✈️" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        var wallet = body!.Wallets.Single(x => x.Id == create!.Id);
        wallet.Currency.Should().Be("EUR");
        wallet.Icon.Should().Be("✈️");
    }
}
