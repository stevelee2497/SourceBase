using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Categories;
using SourceBase.Application.Features.Transactions;
using SourceBase.Application.Features.Transfers;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transactions;

public class UpdateTransactionTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TXN-UPDATE-001: UpdateTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            amount = 50m,
            type = "Income",
            date = "2025-01-22",
            categoryId = Guid.NewGuid(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-UPDATE-002: UpdateTransaction_WithValidData_ReturnsOk")]
    public async Task UpdateTransaction_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var originalCategory = incomeCategories!.First(x => x.IsSystem);

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var updatedCategory = expenseCategories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Income", date = "2025-01-23", note = "Original note", categoryId = originalCategory.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 55m,
            type = "Expense",
            date = "2025-02-02",
            note = "Updated note",
            categoryId = updatedCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateTransactionResponse>();
        body!.Id.Should().Be(transaction.Id);

        var transactionResponse = await client.GetAsync(GetTransactionEndpoint.Route.WithId(transaction.Id));
        var updated = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updated!.Amount.Should().Be(55m);
        updated.Type.Should().Be(TransactionType.Expense);
        updated.Date.Should().Be(new DateOnly(2025, 2, 2));
        updated.Note.Should().Be("Updated note");
        updated.CategoryId.Should().Be(updatedCategory.Id);
    }

    [Fact(DisplayName = "TXN-UPDATE-003: UpdateTransaction_WhenAmountChanges_RecomputesWalletBalance")]
    public async Task UpdateTransaction_WhenAmountChanges_RecomputesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 10m, type = "Income", date = "2025-01-24", note = $"Txn_{Guid.NewGuid():N}", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 40m,
            type = "Income",
            date = "2025-01-24",
            note = "Adjusted",
            categoryId = category.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(140m);
    }

    [Fact(DisplayName = "TXN-UPDATE-004: UpdateTransaction_WhenTypeChanges_RecomputesWalletBalance")]
    public async Task UpdateTransaction_WhenTypeChanges_RecomputesWalletBalance()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var incomeCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var incomeCategories = await incomeCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategory = incomeCategories!.First(x => x.IsSystem);

        var expenseCatResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var expenseCategories = await expenseCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategory = expenseCategories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 30m, type = "Income", date = "2025-01-25", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategory.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 30m,
            type = "Expense",
            date = "2025-01-25",
            note = "Converted to expense",
            categoryId = expenseCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(wallet.Id));
        var walletData = await walletBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletData!.Balance.Should().Be(70m);
    }

    [Fact(DisplayName = "TXN-UPDATE-005: UpdateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 30m, type = "Income", date = "2025-01-26", note = $"Txn_{Guid.NewGuid():N}", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var zeroResponse = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 0m,
            type = "Income",
            date = "2025-01-26",
            note = "Zero",
            categoryId = category.Id,
        });
        var negativeResponse = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = -5m,
            type = "Income",
            date = "2025-01-26",
            note = "Negative",
            categoryId = category.Id,
        });

        // Assert
        zeroResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        negativeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-UPDATE-006: UpdateTransaction_WithUnknownId_ReturnsNotFound")]
    public async Task UpdateTransaction_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            amount = 30m,
            type = "Income",
            date = "2025-01-27",
            categoryId = incomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-UPDATE-007: UpdateTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task UpdateTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var incomeCategoryId = categories!.First(x => x.IsSystem).Id;

        var txnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Income", date = "2025-01-28", note = $"Txn_{Guid.NewGuid():N}", categoryId = incomeCategoryId });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        var otherCatResponse = await otherClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var otherCategories = await otherCatResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var otherIncomeCategoryId = otherCategories!.First(x => x.IsSystem).Id;

        // Act
        var response = await otherClient.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            amount = 50m,
            type = "Income",
            date = "2025-01-28",
            categoryId = otherIncomeCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-UPDATE-008: UpdateTransaction_WithTransferTransaction_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithTransferTransaction_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var fromWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var fromWallet = await fromWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var toWalletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var toWallet = await toWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var transferResponse = await client.PostAsJsonAsync(CreateTransferEndpoint.Route, new { fromWalletId = fromWallet!.Id, toWalletId = toWallet!.Id, amount = 30m, date = "2025-01-29", note = $"Transfer_{Guid.NewGuid():N}" });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        var transferTransactionId = (await (await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={fromWallet!.Id}&limit=100")).Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>())!.Items.Single(x => x.IsTransfer).Id;

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Expense");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var expenseCategoryId = categories!.First(x => x.IsSystem).Id;

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transferTransactionId), new
        {
            amount = 35m,
            type = "Expense",
            date = "2025-01-29",
            note = "Updated transfer",
            categoryId = expenseCategoryId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Transfer transactions cannot be edited directly");
    }

    [Fact(DisplayName = "TXN-UPDATE-009: UpdateTransaction_WithWalletId_MovesTransactionAndRecalculatesBalances")]
    public async Task UpdateTransaction_WithWalletId_MovesTransactionAndRecalculatesBalances()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletAResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 100m, currency = "USD", icon = "💳" });
        var walletA = await walletAResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletBResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 50m, currency = "USD", icon = "💳" });
        var walletB = await walletBResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = walletA!.Id, amount = 30m, type = "Income", date = "2025-03-01", note = $"Txn_{Guid.NewGuid():N}", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            walletId = walletB!.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var walletABody = await client.GetAsync(GetWalletEndpoint.Route.WithId(walletA.Id));
        var walletAData = await walletABody.Content.ReadFromJsonAsync<WalletResponse>();
        walletAData!.Balance.Should().Be(100m);

        var walletBBody = await client.GetAsync(GetWalletEndpoint.Route.WithId(walletB.Id));
        var walletBData = await walletBBody.Content.ReadFromJsonAsync<WalletResponse>();
        walletBData!.Balance.Should().Be(80m);

        var txnDetailResponse = await client.GetAsync(GetTransactionEndpoint.Route.WithId(transaction.Id));
        var txnDetail = await txnDetailResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        txnDetail!.WalletId.Should().Be(walletB.Id);
    }

    [Fact(DisplayName = "TXN-UPDATE-010: UpdateTransaction_WithInvalidWalletId_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithInvalidWalletId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walletResponse = await client.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var wallet = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = wallet!.Id, amount = 20m, type = "Income", date = "2025-03-02", note = $"Txn_{Guid.NewGuid():N}", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            walletId = Guid.NewGuid(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-UPDATE-011: UpdateTransaction_WithOtherUsersWalletId_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithOtherUsersWalletId_ReturnsBadRequest()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownerWalletResponse = await ownerClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var ownerWallet = await ownerWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var otherWalletResponse = await otherClient.PostAsJsonAsync(CreateWalletEndpoint.Route, new { name = $"Wallet_{Guid.NewGuid():N}", initialBalance = 0m, currency = "USD", icon = "💳" });
        var otherWallet = await otherWalletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();

        var catResponse = await ownerClient.GetAsync($"{GetCategoriesEndpoint.Route}?type=Income");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = categories!.First(x => x.IsSystem);

        var txnResponse = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new { walletId = ownerWallet!.Id, amount = 20m, type = "Income", date = "2025-03-03", note = $"Txn_{Guid.NewGuid():N}", categoryId = category.Id });
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await txnResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        // Act
        var response = await ownerClient.PatchAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction!.Id), new
        {
            walletId = otherWallet!.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
