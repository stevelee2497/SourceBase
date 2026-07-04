using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

[EndpointFact(
    Feature = "Wallets",
    Name = "Get Wallet",
    Route = "GET /api/wallets/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve the details of a specific wallet, so that I can view its current balance and metadata.",
    Description = new[]
    {
        "Client provides the wallet `id` (route).",
        "If the wallet doesn't exist or belongs to a different user → `404 Not Found`.",
        "Returns the wallet's full details.",
    })]
public class GetWalletTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-GET-001: without token returns 401")]
    public async Task GetWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-GET-002: owned wallet id returns wallet data")]
    public async Task GetWallet_WithOwnedWalletId_ReturnsWalletData()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Travel_{Guid.NewGuid():N}", initialBalance = 200m, currency = "GBP", icon = "✈️" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create!.Id, amount = 50m, type = "Income", date = "2025-01-12", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WalletResponse>();
        body!.Id.ShouldBe(create.Id);
        body.Currency.ShouldBe("GBP");
        body.Icon.ShouldBe("✈️");
        body.Balance.ShouldBe(250m);
    }

    [Fact(DisplayName = "WALLETS-GET-003: unknown id returns 404")]
    public async Task GetWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-GET-004: other user's wallet returns 404")]
    public async Task GetWallet_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await otherClient.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-GET-005: balance accurately reflects multiple transactions")]
    public async Task GetWallet_BalanceReflectsMultipleTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = expenseCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create!.Id, amount = 50m, type = "Income", date = "2025-01-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create.Id, amount = 30m, type = "Expense", date = "2025-01-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create.Id, amount = 20m, type = "Income", date = "2025-01-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        // Act
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WalletResponse>();
        body!.Balance.ShouldBe(140m); // 100 + 50 - 30 + 20
    }
}
