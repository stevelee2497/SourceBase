using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Wallets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

public class CreateWalletTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-CREATE-001: CreateWallet_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance = 100m,
            currency = "USD",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-CREATE-002: CreateWallet_WithValidData_ReturnsOkAndId")]
    public async Task CreateWallet_WithValidData_ReturnsOkAndId()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Primary_{Guid.NewGuid():N}",
            initialBalance = 100m,
            currency = "USD",
            icon = "💳",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateWalletResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "WALLETS-CREATE-003: CreateWallet_WithMissingName_ReturnsBadRequest")]
    public async Task CreateWallet_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            initialBalance = 100m,
            currency = "USD",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "WALLETS-CREATE-004: CreateWallet_WithMissingCurrency_ReturnsBadRequest")]
    public async Task CreateWallet_WithMissingCurrency_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"NoCurrency_{Guid.NewGuid():N}",
            initialBalance = 100m,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "WALLETS-CREATE-005: CreateWallet_WithNegativeInitialBalance_ReturnsOk")]
    public async Task CreateWallet_WithNegativeInitialBalance_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance = -25m,
            currency = "USD",
            icon = "💳",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Balance.Should().Be(-25m);
        wallet.InitialBalance.Should().Be(-25m);
    }

    [Fact(DisplayName = "WALLETS-CREATE-006: CreateWallet_WithoutTransactions_HasBalanceEqualToInitialBalance")]
    public async Task CreateWallet_WithoutTransactions_HasBalanceEqualToInitialBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance = 123.45m,
            currency = "EUR",
            icon = "💳",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Balance.Should().Be(123.45m);
        wallet.Currency.Should().Be("EUR");
    }

    [Fact(DisplayName = "WALLETS-CREATE-007: CreateWallet_WithAuthenticatedUser_SetsWalletOwnership")]
    public async Task CreateWallet_WithAuthenticatedUser_SetsWalletOwnership()
    {
        // Arrange
        var email = $"wallet_owner_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");

        // Act
        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance = 0m,
            currency = "USD",
            icon = "💳",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Assert
        var data = await factory.WithDbContextAsync(async db => new
        {
            Wallet = await db.Wallets.SingleAsync(x => x.Id == create!.Id),
            UserId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync()
        });
        data.Wallet.UserId.Should().Be(data.UserId);
    }

    [Fact(DisplayName = "WALLETS-CREATE-008: CreateWallet_WithZeroInitialBalance_HasBalanceOfZero")]
    public async Task CreateWallet_WithZeroInitialBalance_HasBalanceOfZero()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance = 0m,
            currency = "USD",
            icon = "💳",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Balance.Should().Be(0m);
        wallet.InitialBalance.Should().Be(0m);
    }

    [Fact(DisplayName = "WALLETS-CREATE-009: CreateWallet_WithNoIcon_StillCreatesWallet")]
    public async Task CreateWallet_WithNoIcon_StillCreatesWallet()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            initialBalance = 50m,
            currency = "USD",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Id.Should().Be(create.Id);
        wallet.Balance.Should().Be(50m);
    }
}
