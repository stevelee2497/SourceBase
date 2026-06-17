using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
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
        var response = await client.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Updated_{Guid.NewGuid():N}",
            icon = "🏦",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-002: UpdateWallet_WithValidData_ReturnsOk")]
    public async Task UpdateWallet_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var updatedName = $"Updated_{Guid.NewGuid():N}";

        // Act
        var response = await client.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
        {
            name = updatedName,
            icon = "🏦",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateWalletResponse>();
        body!.Id.ShouldBe(create.Id);

        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Name.ShouldBe(updatedName);
        wallet.Icon.ShouldBe("🏦");
    }

    [Fact(DisplayName = "WALLETS-UPDATE-003: UpdateWallet_WithOnlyIcon_ReturnsOkAndKeepsName")]
    public async Task UpdateWallet_WithOnlyIcon_ReturnsOkAndKeepsName()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var originalName = $"Wallet_{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = originalName, initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
        {
            icon = "🏦",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Name.ShouldBe(originalName);
        wallet.Icon.ShouldBe("🏦");
    }

    [Fact(DisplayName = "WALLETS-UPDATE-004: UpdateWallet_WithOtherUsersWallet_ReturnsNotFound")]
    public async Task UpdateWallet_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await otherClient.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
        {
            name = $"Hacked_{Guid.NewGuid():N}",
            icon = "🚫",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-005: UpdateWallet_WithUnknownId_ReturnsNotFound")]
    public async Task UpdateWallet_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Unknown_{Guid.NewGuid():N}",
            icon = "🏦",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-006: UpdateWallet_DoesNotChangeComputedBalance")]
    public async Task UpdateWallet_DoesNotChangeComputedBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = incomeCategories!.First(x => x.IsSystem).Id;

        await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = create!.Id, amount = 25m, type = "Income", date = "2025-01-13", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });

        var beforeResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var before = await beforeResponse.Content.ReadFromJsonAsync<WalletResponse>();

        // Act
        var updateResponse = await client.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create.Id), new
        {
            name = $"Renamed_{Guid.NewGuid():N}",
            icon = "🏦",
        });

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var afterResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var after = await afterResponse.Content.ReadFromJsonAsync<WalletResponse>();
        after!.Balance.ShouldBe(before!.Balance);
        after.Balance.ShouldBe(125m);
    }

    [Fact(DisplayName = "WALLETS-UPDATE-007: UpdateWallet_WithNullIcon_KeepsExistingIcon")]
    public async Task UpdateWallet_WithNullIcon_KeepsExistingIcon()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var newName = $"Wallet_{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var create = await createResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(create!.Id), new
        {
            name = newName,
            icon = (string?)null,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var walletResponse = await client.GetAsync(GetWalletEndpoint.Route.WithId(create.Id));
        var wallet = await walletResponse.Content.ReadFromJsonAsync<WalletResponse>();
        wallet!.Name.ShouldBe(newName);
        wallet.Icon.ShouldBe("💳");
    }

    [Fact(DisplayName = "WALLETS-UPDATE-008: UpdateWallet_WithEmptyId_ReturnsBadRequest")]
    public async Task UpdateWallet_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"wallet_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PatchAsJsonAsync(UpdateWalletEndpoint.Route.WithId(Guid.Empty), new
        {
            name = "Test",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
