using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

[EndpointFact(
    Feature = "Wallets",
    Name = "Create Wallet",
    Route = "POST /api/wallets",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to create a named wallet with an initial balance and currency, so that I can start tracking my finances separately per account (e.g. cash, bank, savings).",
    Description = new[]
    {
        "Client sends `name` (required, max 100 characters), `initialBalance` (required, default `0`), `currency` (required, e.g. `\"USD\"`), and optionally `icon` (emoji or icon name string).",
        "The wallet is created and associated with the authenticated user.",
        "Returns the new wallet's `Id`.",
    })]
public class CreateWalletTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-CREATE-001: create wallet without token return 401")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-CREATE-002: create wallet with valid data returns ok and id")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateWalletResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "WALLETS-CREATE-003: create wallet with missing name return 400")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "WALLETS-CREATE-004: create wallet with missing currency return 400")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "WALLETS-CREATE-005: create wallet with negative initial balance return 200")]
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
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Balance.ShouldBe(-25m);
        wallet.InitialBalance.ShouldBe(-25m);
    }

    [Fact(DisplayName = "WALLETS-CREATE-006: create wallet without transactions has balance equal to initial balance")]
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
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Balance.ShouldBe(123.45m);
        wallet.Currency.ShouldBe("EUR");
    }

    [Fact(DisplayName = "WALLETS-CREATE-007: create wallet with authenticated user sets wallet ownership")]
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
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Assert
        var data = await factory.WithDbContextAsync(async db => new
        {
            Wallet = await db.Wallets.SingleAsync(x => x.Id == create!.Id),
            UserId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync()
        });
        data.Wallet.UserId.ShouldBe(data.UserId);
    }

    [Fact(DisplayName = "WALLETS-CREATE-008: create wallet with zero initial balance has balance of zero")]
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
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Balance.ShouldBe(0m);
        wallet.InitialBalance.ShouldBe(0m);
    }

    [Fact(DisplayName = "WALLETS-CREATE-009: create wallet with no icon still creates wallet")]
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
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create!.Id));

        // Assert
        walletResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Id.ShouldBe(create.Id);
        wallet.Balance.ShouldBe(50m);
    }
}
