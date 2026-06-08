using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

public class DeleteWalletTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-DELETE-001: DeleteWallet_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-DELETE-002: DeleteWallet_WithOwnedWallet_ReturnsOk")]
    public async Task DeleteWallet_WithOwnedWallet_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteWalletResponse>();
        body!.Success.Should().BeTrue();

        var getResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-003: DeleteWallet_WithOtherUsersWallet_ReturnsNotFound")]
    public async Task DeleteWallet_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-004: DeleteWallet_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-005: DeleteWallet_RemovesAssociatedTransactions")]
    public async Task DeleteWallet_RemovesAssociatedTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));
        walletResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var txnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={create.Id}&limit=100");
        var txns = await txnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        txns!.Total.Should().Be(0);
    }

    [Fact(DisplayName = "WALLETS-DELETE-006: DeleteWallet_DeletedWalletExcludedFromList")]
    public async Task DeleteWallet_DeletedWalletExcludedFromList()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create!.Id));

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.Should().NotContain(x => x.Id == create.Id);
    }
}
