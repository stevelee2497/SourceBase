using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Transfers;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transfers;

[EndpointFact(
    Feature = "Transfers",
    Name = "Create Transfer",
    Route = "POST /api/transfers",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to record a transfer of money between two of my wallets, so that it is not counted as income or expense and both wallet balances are correctly computed.",
    Description = new[]
    {
        "Client sends `fromWalletId` (required), `toWalletId` (required), `amount` (required, positive), `date` (required), optional `note`.",
        "`fromWalletId` and `toWalletId` must be different → `400 Bad Request` otherwise.",
        "Both wallets must exist and belong to the current user → `404 Not Found` otherwise.",
        "Two linked transactions are created internally: an Expense in `fromWallet` and an Income in `toWallet`. Both are flagged as transfer transactions (not editable or deletable directly).",
        "A `TransferEntity` record is created linking both transactions.",
        "Returns the new transfer's `Id`.",
    })]
public class CreateTransferTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TRANSFER-CREATE-001: create transfer without token return 401")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-002: create transfer with valid data updates both wallet balances")]
    public async Task CreateTransfer_WithValidData_UpdatesBothWalletBalances()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        fromWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        toWalletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet!.Id,
            toWalletId = toWallet!.Id,
            amount = 30m,
            date = "2025-04-02",
            note = $"Transfer_{Guid.NewGuid():N}",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTransferResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);

        var fromWalletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(fromWallet.Id));
        var fromWalletData = await fromWalletBody.Content.ReadFromJsonAsync<WalletResponse>();
        fromWalletData!.Balance.ShouldBe(70m);

        var toWalletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(toWallet.Id));
        var toWalletData = await toWalletBody.Content.ReadFromJsonAsync<WalletResponse>();
        toWalletData!.Balance.ShouldBe(80m);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-003: create transfer with same wallets return 400")]
    public async Task CreateTransfer_WithSameWallets_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = wallet!.Id,
            toWalletId = wallet.Id,
            amount = 10m,
            date = "2025-04-03",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-004: create transfer with other users from wallet return 400")]
    public async Task CreateTransfer_WithOtherUsersFromWallet_ReturnsBadRequest()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var otherWalletResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var otherWallet = await otherWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var ownWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var ownWallet = await ownWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = otherWallet!.Id,
            toWalletId = ownWallet!.Id,
            amount = 10m,
            date = "2025-04-04",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-005: create transfer with other users to wallet return 400")]
    public async Task CreateTransfer_WithOtherUsersToWallet_ReturnsBadRequest()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var ownWallet = await ownWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var otherWalletResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var otherWallet = await otherWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = ownWallet!.Id,
            toWalletId = otherWallet!.Id,
            amount = 10m,
            date = "2025-04-05",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-006: create transfer with zero or negative amount return 400")]
    public async Task CreateTransfer_WithZeroOrNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var zeroResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet!.Id,
            toWalletId = toWallet!.Id,
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
        zeroResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        negativeResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-007: create transfer with missing date return 400")]
    public async Task CreateTransfer_WithMissingDate_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet!.Id,
            toWalletId = toWallet!.Id,
            amount = 10m,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-008: create transfer creates linked transfer transactions")]
    public async Task CreateTransfer_CreatesLinkedTransferTransactions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 35m, date = "2025-04-07", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Assert
        var fromTxnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={fromWallet!.Id}&limit=100");
        var fromTxns = await fromTxnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        fromTxns!.Items.ShouldContain(x => x.Type == TransactionType.Expense && x.IsTransfer);

        var toTxnsResponse = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={toWallet!.Id}&limit=100");
        var toTxns = await toTxnsResponse.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        toTxns!.Items.ShouldContain(x => x.Type == TransactionType.Income && x.IsTransfer);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-009: create transfer with missing from wallet return 400")]
    public async Task CreateTransfer_WithMissingFromWallet_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            toWalletId = toWallet!.Id,
            amount = 10m,
            date = "2025-04-08",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-010: create transfer with unknown from wallet return 400")]
    public async Task CreateTransfer_WithUnknownFromWallet_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = Guid.NewGuid(),
            toWalletId = toWallet!.Id,
            amount = 10m,
            date = "2025-04-09",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TRANSFER-CREATE-011: create transfer with unknown to wallet return 400")]
    public async Task CreateTransfer_WithUnknownToWallet_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new
        {
            fromWalletId = fromWallet!.Id,
            toWalletId = Guid.NewGuid(),
            amount = 10m,
            date = "2025-04-10",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
