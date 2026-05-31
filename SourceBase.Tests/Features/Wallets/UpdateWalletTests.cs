using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Categories;
using SourceBase.Api.Features.Transactions;
using SourceBase.Api.Features.Wallets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Wallets;

public class UpdateWalletTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "WALLETS-UPDATE-001: UpdateWallet_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateWallet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Updated_{Guid.NewGuid():N}",
            icon = "🏦",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-002: UpdateWallet_WithValidData_ReturnsOk")]
    public async Task UpdateWallet_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var updatedName = $"Updated_{Guid.NewGuid():N}";

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
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
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
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
        var ownerClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await otherClient.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
        {
            name = $"Hacked_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-005: UpdateWallet_WithUnknownId_ReturnsNotFound")]
    public async Task UpdateWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Unknown_{Guid.NewGuid():N}",
            icon = "🏦",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-006: UpdateWallet_DoesNotChangeComputedBalance")]
    public async Task UpdateWallet_DoesNotChangeComputedBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create!.Id, amount = 25m, type = "Income", date = "2025-01-13", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        var beforeResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var before = await beforeResponse.Content.ReadFromJsonAsync<WalletResponse>();

        // Act
        var updateResponse = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create.Id), new
        {
            name = $"Renamed_{Guid.NewGuid():N}",
            icon = "🏦",
        });

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var after = await afterResponse.Content.ReadFromJsonAsync<WalletResponse>();
        after!.Balance.Should().Be(before!.Balance);
        after.Balance.Should().Be(125m);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-007: UpdateWallet_WithNullIcon_ClearsIcon")]
    public async Task UpdateWallet_WithNullIcon_ClearsIcon()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
        {
            name = $"Wallet_{Guid.NewGuid():N}",
            icon = (string?)null,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Icon.Should().BeNullOrEmpty();
    }
}
