using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Categories;
using SourceBase.Api.Features.Transactions;
using SourceBase.Api.Features.Transfers;
using SourceBase.Api.Features.Wallets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Transactions;

public class TransactionCrudTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TXN-CREATE-001: CreateTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = Guid.NewGuid(),
            amount = 100m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-01",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-CREATE-002: CreateTransaction_WithIncome_UpdatesWalletBalance")]
    public async Task CreateTransaction_WithIncome_UpdatesWalletBalance()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 100m);
        var incomeCategory = await GetSystemCategoryAsync(client, CategoryType.Income);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = 25m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-01",
            note = $"Income_{Guid.NewGuid():N}",
            categoryId = incomeCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        body!.Id.Should().NotBeEmpty();

        var walletBody = await GetWalletAsync(client, wallet.Id);
        walletBody.Balance.Should().Be(125m);
    }

    [Fact(DisplayName = "TXN-CREATE-003: CreateTransaction_WithExpense_UpdatesWalletBalance")]
    public async Task CreateTransaction_WithExpense_UpdatesWalletBalance()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 100m);
        var expenseCategory = await GetSystemCategoryAsync(client, CategoryType.Expense);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = 30m,
            type = TransactionType.Expense.ToString(),
            date = "2025-01-02",
            note = $"Expense_{Guid.NewGuid():N}",
            categoryId = expenseCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        body!.Id.Should().NotBeEmpty();

        var walletBody = await GetWalletAsync(client, wallet.Id);
        walletBody.Balance.Should().Be(70m);
    }

    [Fact(DisplayName = "TXN-CREATE-004: CreateTransaction_WithMissingWalletId_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingWalletId_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            amount = 20m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-03",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-005: CreateTransaction_WithMissingAmount_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            type = TransactionType.Income.ToString(),
            date = "2025-01-04",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-006: CreateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest")]
    public async Task CreateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);

        // Act
        var zeroResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = 0m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-05",
        });
        var negativeResponse = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = -1m,
            type = TransactionType.Expense.ToString(),
            date = "2025-01-05",
        });

        // Assert
        zeroResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        negativeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-007: CreateTransaction_WithMissingDate_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingDate_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = 20m,
            type = TransactionType.Income.ToString(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-008: CreateTransaction_WithMissingType_ReturnsBadRequest")]
    public async Task CreateTransaction_WithMissingType_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = 20m,
            date = "2025-01-06",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TXN-CREATE-009: CreateTransaction_WithOtherUsersWallet_ReturnsNotFound")]
    public async Task CreateTransaction_WithOtherUsersWallet_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var otherWallet = await CreateWalletAsync(otherClient, 0m);

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = otherWallet.Id,
            amount = 20m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-07",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-CREATE-010: CreateTransaction_WithOtherUsersCategory_ReturnsNotFound")]
    public async Task CreateTransaction_WithOtherUsersCategory_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(ownerClient, 0m);
        var otherCategory = await CreateCustomCategoryAsync(otherClient, CategoryType.Expense);

        // Act
        var response = await ownerClient.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = 20m,
            type = TransactionType.Expense.ToString(),
            date = "2025-01-08",
            categoryId = otherCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-CREATE-011: CreateTransaction_WithoutCategory_AllowsNullCategory")]
    public async Task CreateTransaction_WithoutCategory_AllowsNullCategory()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);

        // Act
        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId = wallet.Id,
            amount = 45m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-09",
            note = $"NoCategory_{Guid.NewGuid():N}",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTransactionResponse>();
        var transactionResponse = await client.GetAsync(GetTransactionEndpoint.Route.WithId(body!.Id));
        var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.CategoryId.Should().BeNull();
        transaction.CategoryName.Should().BeNull();
    }

    [Fact(DisplayName = "TXN-GET-ALL-001: GetTransactions_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTransactions_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransactionsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-GET-ALL-002: GetTransactions_WithOwnedTransactions_ReturnsOk")]
    public async Task GetTransactions_WithOwnedTransactions_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var transaction = await CreateTransactionAsync(client, wallet.Id, 20m, TransactionType.Income, "2025-01-10");

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().Contain(x => x.Id == transaction.Id);
        body.Total.Should().Be(1);
    }

    [Fact(DisplayName = "TXN-GET-ALL-003: GetTransactions_WithMultipleUsers_ReturnsOnlyCurrentUsersTransactions")]
    public async Task GetTransactions_WithMultipleUsers_ReturnsOnlyCurrentUsersTransactions()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var ownerWallet = await CreateWalletAsync(ownerClient, 0m);
        var otherWallet = await CreateWalletAsync(otherClient, 0m);
        var ownTransaction = await CreateTransactionAsync(ownerClient, ownerWallet.Id, 20m, TransactionType.Income, "2025-01-11");
        await CreateTransactionAsync(otherClient, otherWallet.Id, 15m, TransactionType.Income, "2025-01-11");

        // Act
        var response = await ownerClient.GetAsync($"{GetTransactionsEndpoint.Route}?limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == ownTransaction.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-004: GetTransactions_WithWalletFilter_ReturnsMatchingTransactions")]
    public async Task GetTransactions_WithWalletFilter_ReturnsMatchingTransactions()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var walletA = await CreateWalletAsync(client, 0m);
        var walletB = await CreateWalletAsync(client, 0m);
        var transactionA = await CreateTransactionAsync(client, walletA.Id, 10m, TransactionType.Income, "2025-01-12");
        await CreateTransactionAsync(client, walletB.Id, 20m, TransactionType.Income, "2025-01-12");

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?walletId={walletA.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == transactionA.Id && x.WalletId == walletA.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-005: GetTransactions_WithIncomeTypeFilter_ReturnsOnlyIncomeTransactions")]
    public async Task GetTransactions_WithIncomeTypeFilter_ReturnsOnlyIncomeTransactions()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, wallet.Id, 10m, TransactionType.Income, "2025-01-13");
        await CreateTransactionAsync(client, wallet.Id, 5m, TransactionType.Expense, "2025-01-13");

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?type={TransactionType.Income}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().OnlyContain(x => x.Type == TransactionType.Income);
    }

    [Fact(DisplayName = "TXN-GET-ALL-006: GetTransactions_WithExpenseTypeFilter_ReturnsOnlyExpenseTransactions")]
    public async Task GetTransactions_WithExpenseTypeFilter_ReturnsOnlyExpenseTransactions()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, wallet.Id, 10m, TransactionType.Income, "2025-01-14");
        await CreateTransactionAsync(client, wallet.Id, 5m, TransactionType.Expense, "2025-01-14");

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?type={TransactionType.Expense}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().OnlyContain(x => x.Type == TransactionType.Expense);
    }

    [Fact(DisplayName = "TXN-GET-ALL-007: GetTransactions_WithDateRangeFilter_ReturnsTransactionsWithinRange")]
    public async Task GetTransactions_WithDateRangeFilter_ReturnsTransactionsWithinRange()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, wallet.Id, 10m, TransactionType.Income, "2025-01-01");
        var inRange = await CreateTransactionAsync(client, wallet.Id, 15m, TransactionType.Income, "2025-01-15");
        await CreateTransactionAsync(client, wallet.Id, 20m, TransactionType.Income, "2025-02-01");

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?dateFrom=2025-01-10&dateTo=2025-01-20&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == inRange.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-008: GetTransactions_WithCategoryFilter_ReturnsMatchingTransactions")]
    public async Task GetTransactions_WithCategoryFilter_ReturnsMatchingTransactions()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var firstCategory = await CreateCustomCategoryAsync(client, CategoryType.Expense);
        var secondCategory = await CreateCustomCategoryAsync(client, CategoryType.Expense);
        var matching = await CreateTransactionAsync(client, wallet.Id, 10m, TransactionType.Expense, "2025-01-16", firstCategory.Id);
        await CreateTransactionAsync(client, wallet.Id, 5m, TransactionType.Expense, "2025-01-16", secondCategory.Id);

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?categoryId={firstCategory.Id}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == matching.Id && x.CategoryId == firstCategory.Id);
    }

    [Fact(DisplayName = "TXN-GET-ALL-009: GetTransactions_WithPagination_ReturnsCorrectSubset")]
    public async Task GetTransactions_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        await CreateTransactionAsync(client, wallet.Id, 10m, TransactionType.Income, "2025-01-01");
        var second = await CreateTransactionAsync(client, wallet.Id, 20m, TransactionType.Income, "2025-01-02");
        await CreateTransactionAsync(client, wallet.Id, 30m, TransactionType.Income, "2025-01-03");

        // Act
        var response = await client.GetAsync($"{GetTransactionsEndpoint.Route}?page=2&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TransactionResponse>>();
        body!.Total.Should().Be(3);
        body.Items.Should().ContainSingle();
        body.Items[0].Id.Should().Be(second.Id);
    }

    [Fact(DisplayName = "TXN-GET-001: GetTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-GET-002: GetTransaction_WithOwnedTransaction_ReturnsTransactionData")]
    public async Task GetTransaction_WithOwnedTransaction_ReturnsTransactionData()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var category = await GetSystemCategoryAsync(client, CategoryType.Income);
        var created = await CreateTransactionAsync(client, wallet.Id, 40m, TransactionType.Income, "2025-01-20", category.Id, "Salary payment");

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(created.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.Id.Should().Be(created.Id);
        body.WalletId.Should().Be(wallet.Id);
        body.WalletName.Should().StartWith("Wallet_");
        body.CategoryId.Should().Be(category.Id);
        body.CategoryName.Should().Be(category.Name);
        body.Type.Should().Be(TransactionType.Income);
        body.Note.Should().Be("Salary payment");
        body.IsTransfer.Should().BeFalse();
    }

    [Fact(DisplayName = "TXN-GET-003: GetTransaction_WithUnknownId_ReturnsNotFound")]
    public async Task GetTransaction_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.GetAsync(GetTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-GET-004: GetTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task GetTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(ownerClient, 0m);
        var transaction = await CreateTransactionAsync(ownerClient, wallet.Id, 10m, TransactionType.Income, "2025-01-21");

        // Act
        var response = await otherClient.GetAsync(GetTransactionEndpoint.Route.WithId(transaction.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-UPDATE-001: UpdateTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            amount = 50m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-22",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-UPDATE-002: UpdateTransaction_WithValidData_ReturnsOk")]
    public async Task UpdateTransaction_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var originalCategory = await GetSystemCategoryAsync(client, CategoryType.Income);
        var updatedCategory = await GetSystemCategoryAsync(client, CategoryType.Expense);
        var transaction = await CreateTransactionAsync(client, wallet.Id, 20m, TransactionType.Income, "2025-01-23", originalCategory.Id, "Original note");

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = 55m,
            type = TransactionType.Expense.ToString(),
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
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 100m);
        var category = await GetSystemCategoryAsync(client, CategoryType.Income);
        var transaction = await CreateTransactionAsync(client, wallet.Id, 10m, TransactionType.Income, "2025-01-24", category.Id);

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = 40m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-24",
            note = "Adjusted",
            categoryId = category.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await GetWalletAsync(client, wallet.Id);
        walletBody.Balance.Should().Be(140m);
    }

    [Fact(DisplayName = "TXN-UPDATE-004: UpdateTransaction_WhenTypeChanges_RecomputesWalletBalance")]
    public async Task UpdateTransaction_WhenTypeChanges_RecomputesWalletBalance()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 100m);
        var incomeCategory = await GetSystemCategoryAsync(client, CategoryType.Income);
        var expenseCategory = await GetSystemCategoryAsync(client, CategoryType.Expense);
        var transaction = await CreateTransactionAsync(client, wallet.Id, 30m, TransactionType.Income, "2025-01-25", incomeCategory.Id);

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = 30m,
            type = TransactionType.Expense.ToString(),
            date = "2025-01-25",
            note = "Converted to expense",
            categoryId = expenseCategory.Id,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await GetWalletAsync(client, wallet.Id);
        walletBody.Balance.Should().Be(70m);
    }

    [Fact(DisplayName = "TXN-UPDATE-005: UpdateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithZeroOrNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 0m);
        var category = await GetSystemCategoryAsync(client, CategoryType.Income);
        var transaction = await CreateTransactionAsync(client, wallet.Id, 30m, TransactionType.Income, "2025-01-26", category.Id);

        // Act
        var zeroResponse = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = 0m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-26",
            note = "Zero",
            categoryId = category.Id,
        });
        var negativeResponse = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = -5m,
            type = TransactionType.Income.ToString(),
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
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            amount = 30m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-27",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-UPDATE-007: UpdateTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task UpdateTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(ownerClient, 0m);
        var transaction = await CreateTransactionAsync(ownerClient, wallet.Id, 20m, TransactionType.Income, "2025-01-28");

        // Act
        var response = await otherClient.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transaction.Id), new
        {
            amount = 50m,
            type = TransactionType.Income.ToString(),
            date = "2025-01-28",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-UPDATE-008: UpdateTransaction_WithTransferTransaction_ReturnsBadRequest")]
    public async Task UpdateTransaction_WithTransferTransaction_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);
        var transfer = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 30m, "2025-01-29");
        var transferTransactionId = await factory.WithDbContextAsync(async db => await db.Transfers.Where(x => x.Id == transfer.Id).Select(x => x.FromTransactionId).SingleAsync());

        // Act
        var response = await client.PutAsJsonAsync(UpdateTransactionEndpoint.Route.WithId(transferTransactionId), new
        {
            amount = 35m,
            type = TransactionType.Expense.ToString(),
            date = "2025-01-29",
            note = "Updated transfer",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Transfer transactions cannot be edited directly");
    }

    [Fact(DisplayName = "TXN-DELETE-001: DeleteTransaction_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteTransaction_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TXN-DELETE-002: DeleteTransaction_WithIncomeTransaction_RecomputesWalletBalance")]
    public async Task DeleteTransaction_WithIncomeTransaction_RecomputesWalletBalance()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 100m);
        var transaction = await CreateTransactionAsync(client, wallet.Id, 25m, TransactionType.Income, "2025-01-30");

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transaction.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await GetWalletAsync(client, wallet.Id);
        walletBody.Balance.Should().Be(100m);
    }

    [Fact(DisplayName = "TXN-DELETE-003: DeleteTransaction_WithExpenseTransaction_RecomputesWalletBalance")]
    public async Task DeleteTransaction_WithExpenseTransaction_RecomputesWalletBalance()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(client, 100m);
        var transaction = await CreateTransactionAsync(client, wallet.Id, 25m, TransactionType.Expense, "2025-01-31");

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transaction.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletBody = await GetWalletAsync(client, wallet.Id);
        walletBody.Balance.Should().Be(100m);
    }

    [Fact(DisplayName = "TXN-DELETE-004: DeleteTransaction_WithTransferTransaction_ReturnsBadRequest")]
    public async Task DeleteTransaction_WithTransferTransaction_ReturnsBadRequest()
    {
        // Arrange
        var client = await CreateUserClientAsync();
        var fromWallet = await CreateWalletAsync(client, 100m);
        var toWallet = await CreateWalletAsync(client, 50m);
        var transfer = await CreateTransferAsync(client, fromWallet.Id, toWallet.Id, 30m, "2025-02-01");
        var transferTransactionId = await factory.WithDbContextAsync(async db => await db.Transfers.Where(x => x.Id == transfer.Id).Select(x => x.FromTransactionId).SingleAsync());

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transferTransactionId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Transfer transactions cannot be deleted directly");
    }

    [Fact(DisplayName = "TXN-DELETE-005: DeleteTransaction_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteTransaction_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await CreateUserClientAsync();

        // Act
        var response = await client.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TXN-DELETE-006: DeleteTransaction_WithOtherUsersTransaction_ReturnsNotFound")]
    public async Task DeleteTransaction_WithOtherUsersTransaction_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await CreateUserClientAsync();
        var otherClient = await CreateUserClientAsync();
        var wallet = await CreateWalletAsync(ownerClient, 0m);
        var transaction = await CreateTransactionAsync(ownerClient, wallet.Id, 25m, TransactionType.Income, "2025-02-02");

        // Act
        var response = await otherClient.DeleteAsync(DeleteTransactionEndpoint.Route.WithId(transaction.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        return await factory.CreateAuthorizedClient($"transaction_user_{Guid.NewGuid():N}@test.com", "Test@1234!");
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

    private async Task<CreateCategoryResponse> CreateCustomCategoryAsync(HttpClient client, CategoryType type)
    {
        var response = await client.PostAsJsonAsync(CreateCategoryEndpoint.Route, new
        {
            name = $"Category_{Guid.NewGuid():N}",
            type = type.ToString(),
            icon = "🏷️",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateCategoryResponse>())!;
    }

    private async Task<CategoryResponse> GetSystemCategoryAsync(HttpClient client, CategoryType type)
    {
        var response = await client.GetAsync($"{GetCategoriesEndpoint.Route}?type={type}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        return categories!.First(x => x.IsSystem && x.Type == type);
    }

    private async Task<CreateTransactionResponse> CreateTransactionAsync(HttpClient client, Guid walletId, decimal amount, TransactionType type, string date, Guid? categoryId = null, string? note = null)
    {
        var effectiveCategoryId = categoryId;
        if (effectiveCategoryId is null)
        {
            var systemCategory = await GetSystemCategoryAsync(client, type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense);
            effectiveCategoryId = systemCategory.Id;
        }

        var response = await client.PostAsJsonAsync(CreateTransactionEndpoint.Route, new
        {
            walletId,
            amount,
            type = type.ToString(),
            date,
            note = note ?? $"Txn_{Guid.NewGuid():N}",
            categoryId = effectiveCategoryId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CreateTransactionResponse>())!;
    }

    private async Task<WalletResponse> GetWalletAsync(HttpClient client, Guid walletId)
    {
        var response = await client.GetAsync(GetWalletEndpoint.Route.WithId(walletId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WalletResponse>())!;
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
}
