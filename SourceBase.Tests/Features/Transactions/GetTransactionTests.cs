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

public class GetTransactionTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TXN-GET-001: GetTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-GET-002: GetTransaction_WithOwnedTransaction_ReturnsTransactionData")]
    public async Task GetTransaction_WithOwnedTransaction_ReturnsTransactionData()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 40m, type = "Income", date = "2025-01-20", note = "Salary payment", categoryId = category.Id });
        txnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(created!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.Id.ShouldBe(created.Id);
        body.WalletId.ShouldBe(wallet.Id);
        body.WalletName.ShouldStartWith("Wallet_");
        body.CategoryId.ShouldBe(category.Id);
        body.CategoryName.ShouldBe(category.Name);
        body.Type.ShouldBe(TransactionType.Income);
        body.Note.ShouldBe("Salary payment");
        body.IsTransfer.ShouldBeFalse();
    }

    [Fact(DisplayName = "TXN-GET-003: GetTransaction_WithUnknownId_ReturnsNotFound")]
    public async Task GetTransaction_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-GET-004: GetTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task GetTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-21", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await otherClient.GetAsync(GetTransactionEndpoint.Route.WithId(transaction!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
