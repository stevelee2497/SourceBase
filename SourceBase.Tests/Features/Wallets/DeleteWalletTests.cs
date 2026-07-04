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
    Name = "Delete Wallet",
    Route = "DELETE /api/wallets/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to delete one of my wallets, so that I can remove accounts I no longer use.",
    Description = new[]
    {
        "Client provides the wallet `id` (route).",
        "If the wallet doesn't exist or belongs to a different user → `404 Not Found`.",
        "The wallet and all its associated transactions are deleted. Any transfer records that reference this wallet are also deleted.",
    })]
public class DeleteWalletTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-DELETE-001: no token returns 401")]
    public async Task DeleteWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-DELETE-002: owned wallet returns 200")]
    public async Task DeleteWallet_WithOwnedWallet_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteWalletResponse>();
        body!.Success.ShouldBeTrue();

        var getResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-003: other user's wallet returns 404")]
    public async Task DeleteWallet_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-004: unknown id returns 404")]
    public async Task DeleteWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-005: delete removes associated transactions")]
    public async Task DeleteWallet_RemovesAssociatedTransactions()
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

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create!.Id, amount = 20m, type = "Income", date = "2025-01-14", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create.Id, amount = 5m, type = "Expense", date = "2025-01-15", note = $"Txn_{Guid.NewGuid():N}", categoryId = expenseCategoryId });

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var txnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={create.Id}&limit=100");
        var txns = await txnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        txns!.Total.ShouldBe(0);
    }

    [Fact(DisplayName = "WALLETS-DELETE-006: deleted wallet is excluded from list")]
    public async Task DeleteWallet_DeletedWalletExcludedFromList()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create!.Id));

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.ShouldNotContain(x => x.Id == create.Id);
    }
}
