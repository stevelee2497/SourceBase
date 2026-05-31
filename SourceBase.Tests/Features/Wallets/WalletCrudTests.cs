using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Categories;
using SourceBase.Api.Features.Transactions;
using SourceBase.Api.Features.Wallets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

public class WalletCrudTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-CREATE-001: CreateWallet_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = UniqueWalletName("Unauthorized"),
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
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = UniqueWalletName("Primary"),
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
        var client = await CreateUserClientAsync();

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
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = UniqueWalletName("NoCurrency"),
            initialBalance = 100m,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "WALLETS-CREATE-005: CreateWallet_WithNegativeInitialBalance_ReturnsOk")]
    public async Task CreateWallet_WithNegativeInitialBalance_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var create = await CreateWalletAsync(client, initialBalance: -25m);
        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));

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
        var client = await CreateUserClientAsync();

        // Act
        var create = await CreateWalletAsync(client, initialBalance: 123.45m, currency: "EUR");
        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));

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
        var create = await CreateWalletAsync(client);

        // Assert
        var data = await factory.WithDbContextAsync(async db => new
        {
            Wallet = await db.Wallets.SingleAsync(x => x.Id == create.Id),
            UserId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync()
        });
        data.Wallet.UserId.Should().Be(data.UserId);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-001: GetWallets_WithoutToken_ReturnsUnauthorized")]
    public async Task GetWallets_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-002: GetWallets_WithOwnedWallets_ReturnsOk")]
    public async Task GetWallets_WithOwnedWallets_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var first = await CreateWalletAsync(client, name: UniqueWalletName("Cash"), initialBalance: 50m);
        var second = await CreateWalletAsync(client, name: UniqueWalletName("Bank"), initialBalance: 75m);

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.Should().Contain(x => x.Id == first.Id);
        body.Wallets.Should().Contain(x => x.Id == second.Id);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-003: GetWallets_WithMultipleUsers_ReturnsOnlyCurrentUsersWallets")]
    public async Task GetWallets_WithMultipleUsers_ReturnsOnlyCurrentUsersWallets()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var ownWallet = await CreateWalletAsync(ownerClient, name: UniqueWalletName("Own"), initialBalance: 10m);
        await CreateWalletAsync(otherClient, name: UniqueWalletName("Other"), initialBalance: 20m);

        // Act
        var response = await ownerClient.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.Should().ContainSingle(x => x.Id == ownWallet.Id);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-004: GetWallets_WithBalances_ReturnsCorrectTotalBalance")]
    public async Task GetWallets_WithBalances_ReturnsCorrectTotalBalance()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var first = await CreateWalletAsync(client, name: UniqueWalletName("Main"), initialBalance: 100m);
        var second = await CreateWalletAsync(client, name: UniqueWalletName("Savings"), initialBalance: 50m);
        await CreateTransactionAsync(client, first.Id, 25m, TransactionType.Income, "2025-01-10");
        await CreateTransactionAsync(client, second.Id, 10m, TransactionType.Expense, "2025-01-11");

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.TotalBalance.Should().Be(165m);
    }

    [Fact(DisplayName = "WALLETS-GET-ALL-005: GetWallets_WithNoWallets_ReturnsEmptyList")]
    public async Task GetWallets_WithNoWallets_ReturnsEmptyList()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.GetAsync(GetWalletsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetWalletsResponse>();
        body!.Wallets.Should().BeEmpty();
        body.TotalBalance.Should().Be(0m);
    }

    [Fact(DisplayName = "WALLETS-GET-001: GetWallet_WithoutToken_ReturnsUnauthorized")]
    public async Task GetWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-GET-002: GetWallet_WithOwnedWalletId_ReturnsWalletData")]
    public async Task GetWallet_WithOwnedWalletId_ReturnsWalletData()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateWalletAsync(client, name: UniqueWalletName("Travel"), initialBalance: 200m, currency: "GBP", icon: "✈️");
        await CreateTransactionAsync(client, create.Id, 50m, TransactionType.Income, "2025-01-12");

        // Act
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WalletResponse>();
        body!.Id.Should().Be(create.Id);
        body.Name.Should().StartWith("Travel_");
        body.Currency.Should().Be("GBP");
        body.Icon.Should().Be("✈️");
        body.Balance.Should().Be(250m);
    }

    [Fact(DisplayName = "WALLETS-GET-003: GetWallet_WithUnknownId_ReturnsNotFound")]
    public async Task GetWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-GET-004: GetWallet_WithOtherUsersWallet_ReturnsNotFound")]
    public async Task GetWallet_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var create = await CreateWalletAsync(ownerClient);

        // Act
        var response = await otherClient.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-001: UpdateWallet_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = UniqueWalletName("Updated"),
            icon = "🏦",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-002: UpdateWallet_WithValidData_ReturnsOk")]
    public async Task UpdateWallet_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateWalletAsync(client, icon: "💳");
        var updatedName = UniqueWalletName("Updated");

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create.Id), new
        {
            name = updatedName,
            icon = "🏦",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateWalletResponse>();
        body!.Id.Should().Be(create.Id);

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Name.Should().Be(updatedName);
        wallet.Icon.Should().Be("🏦");
    }

    [Fact(DisplayName = "WALLETS-UPDATE-003: UpdateWallet_WithMissingName_ReturnsBadRequest")]
    public async Task UpdateWallet_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateWalletAsync(client);

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create.Id), new
        {
            icon = "🏦",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-004: UpdateWallet_WithOtherUsersWallet_ReturnsNotFound")]
    public async Task UpdateWallet_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var create = await CreateWalletAsync(ownerClient);

        // Act
        var response = await otherClient.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create.Id), new
        {
            name = UniqueWalletName("Hacked"),
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-005: UpdateWallet_WithUnknownId_ReturnsNotFound")]
    public async Task UpdateWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = UniqueWalletName("Unknown"),
            icon = "🏦",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-006: UpdateWallet_DoesNotChangeComputedBalance")]
    public async Task UpdateWallet_DoesNotChangeComputedBalance()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateWalletAsync(client, initialBalance: 100m, icon: "💳");
        await CreateTransactionAsync(client, create.Id, 25m, TransactionType.Income, "2025-01-13");

        var beforeResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var before = await beforeResponse.Content.ReadFromJsonAsync<WalletResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create.Id), new
        {
            name = UniqueWalletName("Renamed"),
            icon = "🏦",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var after = await afterResponse.Content.ReadFromJsonAsync<WalletResponse>();
        after!.Balance.Should().Be(before!.Balance);
        after.Balance.Should().Be(125m);
    }

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
        var client = await CreateUserClientAsync();
        var create = await CreateWalletAsync(client);

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create.Id));

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
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var create = await CreateWalletAsync(ownerClient);

        // Act
        var response = await otherClient.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-004: DeleteWallet_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-DELETE-005: DeleteWallet_RemovesAssociatedTransactions")]
    public async Task DeleteWallet_RemovesAssociatedTransactions()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var create = await CreateWalletAsync(client, initialBalance: 100m);
        await CreateTransactionAsync(client, create.Id, 20m, TransactionType.Income, "2025-01-14");
        await CreateTransactionAsync(client, create.Id, 5m, TransactionType.Expense, "2025-01-15");

        // Act
        var response = await client.DeleteAsync(DeleteWalletEndpoint.Route.WithId(create.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var counts = await factory.WithDbContextAsync(async db => new
        {
            WalletExists = await db.Wallets.AnyAsync(x => x.Id == create.Id),
            TransactionCount = await db.Transactions.CountAsync(x => x.WalletId == create.Id)
        });
        counts.WalletExists.Should().BeFalse();
        counts.TransactionCount.Should().Be(0);
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        return await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
    }

    private static string UniqueWalletName(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }

    private async Task<CreateWalletResponse> CreateWalletAsync(HttpClient client, string? name = null, decimal initialBalance = 0m, string currency = "USD", string? icon = "💳")
    {
        var response = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new
        {
            name = name ?? UniqueWalletName("Wallet"),
            initialBalance,
            currency,
            icon,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateWalletResponse>())!;
    }

    private async Task<CreateTransactionResponse> CreateTransactionAsync(HttpClient client, Guid walletId, decimal amount, TransactionType type, string date)
    {
        var category = await GetSystemCategoryAsync(client, type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense);
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId,
            amount,
            type = type.ToString(),
            date,
            note = $"Txn_{Guid.NewGuid():N}",
            categoryId = category.Id,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateTransactionResponse>())!;
    }

    private async Task<CategoryResponse> GetSystemCategoryAsync(HttpClient client, CategoryType type)
    {
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={type}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        return categories!.First(x => x.IsSystem && x.Type == type);
    }
}
