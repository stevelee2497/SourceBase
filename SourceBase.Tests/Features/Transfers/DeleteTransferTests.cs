using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Transfers;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transfers;

public class DeleteTransferTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TRANSFER-DELETE-001: DeleteTransfer_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteTransfer_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-002: DeleteTransfer_WithOwnedTransfer_RemovesLinkedTransactionsAndRestoresBalances")]
    public async Task DeleteTransfer_WithOwnedTransfer_RemovesLinkedTransactionsAndRestoresBalances()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        fromWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        toWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 30m, date = "2025-04-16", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteTransferResponse>();
        body!.Success.Should().BeTrue();

        var fromWalletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(fromWallet.Id));
        var fromWalletData = await fromWalletBody.Content.ReadFromJsonAsync<WalletResponse>();
        fromWalletData!.Balance.Should().Be(100m);

        var toWalletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(toWallet.Id));
        var toWalletData = await toWalletBody.Content.ReadFromJsonAsync<WalletResponse>();
        toWalletData!.Balance.Should().Be(50m);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-003: DeleteTransfer_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteTransfer_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-004: DeleteTransfer_WithOtherUsersTransfer_ReturnsNotFound")]
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
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-005: DeleteTransfer_RemovesBothLinkedTransactions")]
    public async Task DeleteTransfer_RemovesBothLinkedTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 25m, date = "2025-04-18", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfersListResponse = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=100");
        var transfersList = await transfersListResponse.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        transfersList!.Items.Should().NotContain(x => x.Id == transfer.Id);

        var fromTxnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={fromWallet!.Id}&limit=100");
        var fromTxns = await fromTxnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        fromTxns!.Items.Should().NotContain(x => x.IsTransfer);

        var toTxnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={toWallet!.Id}&limit=100");
        var toTxns = await toTxnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        toTxns!.Items.Should().NotContain(x => x.IsTransfer);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-006: DeleteTransfer_DoesNotAffectOtherTransfers")]
    public async Task DeleteTransfer_DoesNotAffectOtherTransfers()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 200m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transfer1Response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 20m, date = "2025-04-19", note = $"Transfer_{Guid.NewGuid():N}" });
        transfer1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer1 = await transfer1Response.Content.ReadFromJsonAsync<CreateTransferResponse>();

        var transfer2Response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet.Id, toWalletId = toWallet.Id, amount = 10m, date = "2025-04-20", note = $"Transfer_{Guid.NewGuid():N}" });
        transfer2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer2 = await transfer2Response.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer1!.Id));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var listResponse = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");
        var body = await listResponse.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == transfer2!.Id);
        body.Items.Should().NotContain(x => x.Id == transfer1.Id);
    }
}
