using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Transfers;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transfers;

[EndpointFact(
    Feature = "Transfers",
    Name = "Delete Transfer",
    Route = "DELETE /api/transfers/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to delete an incorrect transfer, so that the linked transactions are removed and both wallet balances are recomputed correctly.",
    Description = new[]
    {
        "Client provides the transfer `id` (route).",
        "If the transfer doesn't exist or belongs to a different user → `404 Not Found`.",
        "Both linked transactions are deleted. The wallet computed balances automatically reflect the deletion on next query.",
    })]
public class DeleteTransferTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TRANSFER-DELETE-001: delete transfer without token return 401")]
    public async Task DeleteTransfer_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-002: delete transfer with owned transfer removes linked transactions and restores balances")]
    public async Task DeleteTransfer_WithOwnedTransfer_RemovesLinkedTransactionsAndRestoresBalances()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        fromWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        toWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 30m, date = "2025-04-16", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteTransferResponse>();
        body!.Success.ShouldBeTrue();

        var fromWalletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(fromWallet.Id));
        var fromWalletData = await fromWalletBody.Content.ReadFromJsonAsync<WalletResponse>();
        fromWalletData!.Balance.ShouldBe(100m);

        var toWalletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(toWallet.Id));
        var toWalletData = await toWalletBody.Content.ReadFromJsonAsync<WalletResponse>();
        toWalletData!.Balance.ShouldBe(50m);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-003: delete transfer with unknown id return 404")]
    public async Task DeleteTransfer_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-004: delete transfer with other users transfer return 404")]
    public async Task DeleteTransfer_WithOtherUsersTransfer_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await ownerClient.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 10m, date = "2025-04-17", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-005: delete transfer removes both linked transactions")]
    public async Task DeleteTransfer_RemovesBothLinkedTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 25m, date = "2025-04-18", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transfersListResponse = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=100");
        var transfersList = await transfersListResponse.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        transfersList!.Items.ShouldNotContain(x => x.Id == transfer.Id);

        var fromTxnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={fromWallet!.Id}&limit=100");
        var fromTxns = await fromTxnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        fromTxns!.Items.ShouldNotContain(x => x.IsTransfer);

        var toTxnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={toWallet!.Id}&limit=100");
        var toTxns = await toTxnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        toTxns!.Items.ShouldNotContain(x => x.IsTransfer);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-006: delete transfer does not affect other transfers")]
    public async Task DeleteTransfer_DoesNotAffectOtherTransfers()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 200m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transfer1Response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 20m, date = "2025-04-19", note = $"Transfer_{Guid.NewGuid():N}" });
        transfer1Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transfer1 = await transfer1Response.Content.ReadFromJsonAsync<CreateTransferResponse>();

        var transfer2Response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet.Id, toWalletId = toWallet.Id, amount = 10m, date = "2025-04-20", note = $"Transfer_{Guid.NewGuid():N}" });
        transfer2Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transfer2 = await transfer2Response.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer1!.Id));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert
        var listResponse = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");
        var body = await listResponse.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.ShouldContain(x => x.Id == transfer2!.Id);
        body.Items.ShouldNotContain(x => x.Id == transfer1.Id);
    }
}
