using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Transfers;
using SourceBase.Api.Features.Wallets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transfers;

public class TransferTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TRANSFER-CREATE-001: CreateTransfer_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateTransfer_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = Guid.NewGuid(),
            toWalletId = Guid.NewGuid(),
            amount = 10m,
            date = "2025-04-01",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-002: CreateTransfer_WithValidData_UpdatesBothWalletBalances")]
    public async Task CreateTransfer_WithValidData_UpdatesBothWalletBalances()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet.Id,
            toWalletId = toWallet.Id,
            amount = 30m,
            date = "2025-04-02",
            note = $"Transfer_{Guid.NewGuid():N}",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTransferResponse>();
        body!.Id.Should().NotBeEmpty();

        var updatedFromWallet = await GetWalletAsync(client, fromWallet.Id);
        var updatedToWallet = await GetWalletAsync(client, toWallet.Id);
        updatedFromWallet.Balance.Should().Be(70m);
        updatedToWallet.Balance.Should().Be(80m);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-003: CreateTransfer_WithSameWallets_ReturnsBadRequest")]
    public async Task CreateTransfer_WithSameWallets_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 100m);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = wallet.Id,
            toWalletId = wallet.Id,
            amount = 10m,
            date = "2025-04-03",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-004: CreateTransfer_WithOtherUsersFromWallet_ReturnsNotFound")]
    public async Task CreateTransfer_WithOtherUsersFromWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var otherWallet = await CreateWalletAsync(otherClient, 100m);
        var ownWallet = await CreateWalletAsync(ownerClient, 50m);

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = otherWallet.Id,
            toWalletId = ownWallet.Id,
            amount = 10m,
            date = "2025-04-04",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-005: CreateTransfer_WithOtherUsersToWallet_ReturnsNotFound")]
    public async Task CreateTransfer_WithOtherUsersToWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var ownWallet = await CreateWalletAsync(ownerClient, 100m);
        var otherWallet = await CreateWalletAsync(otherClient, 50m);

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = ownWallet.Id,
            toWalletId = otherWallet.Id,
            amount = 10m,
            date = "2025-04-05",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-006: CreateTransfer_WithZeroOrNegativeAmount_ReturnsBadRequest")]
    public async Task CreateTransfer_WithZeroOrNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);

        // Act
        var zeroResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet.Id,
            toWalletId = toWallet.Id,
            amount = 0m,
            date = "2025-04-06",
        });
        var negativeResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet.Id,
            toWalletId = toWallet.Id,
            amount = -1m,
            date = "2025-04-06",
        });

        // Assert
        zeroResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        negativeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-007: CreateTransfer_WithMissingDate_ReturnsBadRequest")]
    public async Task CreateTransfer_WithMissingDate_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet.Id,
            toWalletId = toWallet.Id,
            amount = 10m,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-008: CreateTransfer_CreatesLinkedTransferTransactions")]
    public async Task CreateTransfer_CreatesLinkedTransferTransactions()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);

        // Act
        var transfer = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 35m, "2025-04-07");

        // Assert
        var data = await factory.WithDbContextAsync(async db =>
        {
            var transferEntity = await db.Transfers.SingleAsync(x => x.Id == transfer.Id);
            var transactions = await db.Transactions
                .Where(x => x.Id == transferEntity.FromTransactionId || x.Id == transferEntity.ToTransactionId)
                .ToListAsync();

            return new
            {
                Transfer = transferEntity,
                Transactions = transactions,
            };
        });
        data.Transactions.Should().HaveCount(2);
        data.Transactions.Should().Contain(x => x.Id == data.Transfer.FromTransactionId && x.Type == TransactionType.Expense && x.IsTransfer);
        data.Transactions.Should().Contain(x => x.Id == data.Transfer.ToTransactionId && x.Type == TransactionType.Income && x.IsTransfer);
    }

    [Fact(DisplayName = "TRANSFER-GET-001: GetTransfers_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTransfers_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransfersEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TRANSFER-GET-002: GetTransfers_WithOwnedTransfers_ReturnsOk")]
    public async Task GetTransfers_WithOwnedTransfers_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);
        var transfer = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 15m, "2025-04-08");

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().Contain(x => x.Id == transfer.Id);
        body.Total.Should().Be(1);
    }

    [Fact(DisplayName = "TRANSFER-GET-003: GetTransfers_WithMultipleUsers_ReturnsOnlyCurrentUsersTransfers")]
    public async Task GetTransfers_WithMultipleUsers_ReturnsOnlyCurrentUsersTransfers()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var ownerFrom = await CreateWalletAsync(ownerClient, 100m);
        var ownerTo = await CreateWalletAsync(ownerClient, 50m);
        var otherFrom = await CreateWalletAsync(otherClient, 100m);
        var otherTo = await CreateWalletAsync(otherClient, 50m);
        var ownTransfer = await CreateTransferAsync(ownerClient, ownerFrom.Id, ownerTo.Id, 15m, "2025-04-09");
        await CreateTransferAsync(otherClient, otherFrom.Id, otherTo.Id, 20m, "2025-04-09");

        // Act
        var response = await ownerClient.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == ownTransfer.Id);
    }

    [Fact(DisplayName = "TRANSFER-GET-004: GetTransfers_WithWalletFilter_ReturnsMatchingTransfers")]
    public async Task GetTransfers_WithWalletFilter_ReturnsMatchingTransfers()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var walletA = await CreateWalletAsync(client, 100m);
        var walletB = await CreateWalletAsync(client, 50m);
        var walletC = await CreateWalletAsync(client, 75m);
        var matching = await CreateTransferAsync(client, walletA.Id, walletB.Id, 10m, "2025-04-10");
        await CreateTransferAsync(client, walletB.Id, walletC.Id, 12m, "2025-04-11");

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?walletId={walletA.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == matching.Id);
    }

    [Fact(DisplayName = "TRANSFER-GET-005: GetTransfers_WithDateRange_ReturnsTransfersWithinRange")]
    public async Task GetTransfers_WithDateRange_ReturnsTransfersWithinRange()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);
        await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 10m, "2025-04-01");
        var inRange = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 15m, "2025-04-15");
        await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 20m, "2025-05-01");

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?dateFrom=2025-04-10&dateTo=2025-04-20&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == inRange.Id);
    }

    [Fact(DisplayName = "TRANSFER-GET-006: GetTransfers_WithPagination_ReturnsCorrectSubset")]
    public async Task GetTransfers_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);
        await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 10m, "2025-04-01");
        var second = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 20m, "2025-04-02");
        await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 30m, "2025-04-03");

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?page=2&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Total.Should().Be(3);
        body.Items.Should().ContainSingle();
        body.Items[0].Id.Should().Be(second.Id);
    }

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
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);
        var transfer = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 30m, "2025-04-12");

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteTransferResponse>();
        body!.Success.Should().BeTrue();

        var restoredFromWallet = await GetWalletAsync(client, fromWallet.Id);
        var restoredToWallet = await GetWalletAsync(client, toWallet.Id);
        restoredFromWallet.Balance.Should().Be(100m);
        restoredToWallet.Balance.Should().Be(50m);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-003: DeleteTransfer_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteTransfer_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-004: DeleteTransfer_WithOtherUsersTransfer_ReturnsNotFound")]
    public async Task DeleteTransfer_WithOtherUsersTransfer_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(ownerClient, 100m);
        var toWallet = await CreateWalletAsync(ownerClient, 50m);
        var transfer = await CreateTransferAsync(ownerClient, fromWallet.Id, toWallet.Id, 10m, "2025-04-13");

        // Act
        var response = await otherClient.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TRANSFER-DELETE-005: DeleteTransfer_RemovesBothLinkedTransactions")]
    public async Task DeleteTransfer_RemovesBothLinkedTransactions()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);
        var transfer = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 25m, "2025-04-14");
        var transferData = await factory.WithDbContextAsync(async db => await db.Transfers.SingleAsync(x => x.Id == transfer.Id));

        // Act
        var response = await client.DeleteAsync(DeleteTransferEndpoint.Route.WithId(transfer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var counts = await factory.WithDbContextAsync(async db => new
        {
            TransferExists = await db.Transfers.AnyAsync(x => x.Id == transfer.Id),
            FromTransactionExists = await db.Transactions.AnyAsync(x => x.Id == transferData.FromTransactionId),
            ToTransactionExists = await db.Transactions.AnyAsync(x => x.Id == transferData.ToTransactionId)
        });
        counts.TransferExists.Should().BeFalse();
        counts.FromTransactionExists.Should().BeFalse();
        counts.ToTransactionExists.Should().BeFalse();
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        return await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
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

    private async Task<CreateTransferResponse> CreateTransferAsync(HttpClient client, Guid fromWalletId, Guid toWalletId, decimal amount, string date)
    {
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId,
            toWalletId,
            amount,
            date,
            note = $"Transfer_{Guid.NewGuid():N}",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateTransferResponse>())!;
    }

    private async Task<WalletResponse> GetWalletAsync(HttpClient client, Guid walletId)
    {
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(walletId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WalletResponse>())!;
    }
}
