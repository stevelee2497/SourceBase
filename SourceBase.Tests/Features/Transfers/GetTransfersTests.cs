using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Transfers;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transfers;

public class GetTransfersTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
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
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 15m, date = "2025-04-11", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().Contain(x => x.Id == transfer!.Id);
        body.Total.Should().Be(1);
    }

    [Fact(DisplayName = "TRANSFER-GET-003: GetTransfers_ReturnsTransferDetails")]
    public async Task GetTransfers_ReturnsTransferDetails()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"FromWallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        fromWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"ToWallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        toWalletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var note = $"Transfer_{Guid.NewGuid():N}";
        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 20m, date = "2025-04-12", note });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        var item = body!.Items.Single(x => x.Id == transfer!.Id);
        item.FromWalletId.Should().Be(fromWallet.Id);
        item.ToWalletId.Should().Be(toWallet.Id);
        item.Amount.Should().Be(20m);
        item.Date.Should().Be(new DateOnly(2025, 4, 12));
        item.Note.Should().Be(note);
    }

    [Fact(DisplayName = "TRANSFER-GET-004: GetTransfers_WithMultipleUsers_ReturnsOnlyCurrentUsersTransfers")]
    public async Task GetTransfers_WithMultipleUsers_ReturnsOnlyCurrentUsersTransfers()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownerFromResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var ownerFrom = await ownerFromResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var ownerToResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var ownerTo = await ownerToResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var otherFromResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var otherFrom = await otherFromResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var otherToResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var otherTo = await otherToResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var ownTransferResponse = await ownerClient.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = ownerFrom!.Id, toWalletId = ownerTo!.Id, amount = 15m, date = "2025-04-13", note = $"Transfer_{Guid.NewGuid():N}" });
        ownTransferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownTransfer = await ownTransferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        await otherClient.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = otherFrom!.Id, toWalletId = otherTo!.Id, amount = 20m, date = "2025-04-13", note = $"Transfer_{Guid.NewGuid():N}" });

        // Act
        var response = await ownerClient.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == ownTransfer!.Id);
    }

    [Fact(DisplayName = "TRANSFER-GET-005: GetTransfers_WithWalletFilter_ReturnsMatchingTransfers")]
    public async Task GetTransfers_WithWalletFilter_ReturnsMatchingTransfers()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletAResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var walletA = await walletAResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletBResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var walletB = await walletBResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletCResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 75m, currency = "USD", icon = "💳" });
        var walletC = await walletCResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var matchingResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = walletA!.Id, toWalletId = walletB!.Id, amount = 10m, date = "2025-04-14", note = $"Transfer_{Guid.NewGuid():N}" });
        matchingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var matching = await matchingResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();
        await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = walletB.Id, toWalletId = walletC!.Id, amount = 12m, date = "2025-04-15", note = $"Transfer_{Guid.NewGuid():N}" });

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?walletId={walletA.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == matching!.Id);
    }

    [Fact(DisplayName = "TRANSFER-GET-006: GetTransfers_WithDateRange_ReturnsTransfersWithinRange")]
    public async Task GetTransfers_WithDateRange_ReturnsTransfersWithinRange()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 200m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 10m, date = "2025-04-01", note = $"Transfer_{Guid.NewGuid():N}" });
        var inRangeResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet.Id, toWalletId = toWallet.Id, amount = 15m, date = "2025-04-15", note = $"Transfer_{Guid.NewGuid():N}" });
        inRangeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inRange = await inRangeResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();
        await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet.Id, toWalletId = toWallet.Id, amount = 20m, date = "2025-05-01", note = $"Transfer_{Guid.NewGuid():N}" });

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?dateFrom=2025-04-10&dateTo=2025-04-20&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == inRange!.Id);
    }

    [Fact(DisplayName = "TRANSFER-GET-007: GetTransfers_WithPagination_ReturnsCorrectSubset")]
    public async Task GetTransfers_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 200m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 10m, date = "2025-04-01", note = $"Transfer_{Guid.NewGuid():N}" });
        var secondResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet.Id, toWalletId = toWallet.Id, amount = 20m, date = "2025-04-02", note = $"Transfer_{Guid.NewGuid():N}" });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await secondResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();
        await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet.Id, toWalletId = toWallet.Id, amount = 30m, date = "2025-04-03", note = $"Transfer_{Guid.NewGuid():N}" });

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?page=2&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Total.Should().Be(3);
        body.Items.Should().ContainSingle();
        body.Items[0].Id.Should().Be(second!.Id);
    }

    [Fact(DisplayName = "TRANSFER-GET-008: GetTransfers_WithNoTransfers_ReturnsEmptyList")]
    public async Task GetTransfers_WithNoTransfers_ReturnsEmptyList()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transfer_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync($"{GetTransfersEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransferResponse>>();
        body!.Items.Should().BeEmpty();
        body.Total.Should().Be(0);
    }
}
