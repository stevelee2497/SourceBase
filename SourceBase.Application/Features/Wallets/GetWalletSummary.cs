using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Wallets;

public record GetWalletSummaryRequest();

public record RecentTransactionResponse(Guid Id, decimal Amount, TransactionType Type, DateOnly Date, string? Note, Guid WalletId, string WalletName, Guid? CategoryId, string? CategoryName);

public record GetWalletSummaryResponse(decimal TotalBalance, decimal MonthlyIncome, decimal MonthlyExpense, List<RecentTransactionResponse> RecentTransactions);

public class GetWalletSummaryEndpoint : IEndpoint
{
    public const string Route = "wallets/summary";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetWalletSummaryRequest request, GetWalletSummaryHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Wallets");
}

public class GetWalletSummaryHandler(IDbContext dbContext, ICurrentUser currentUser, IDateTime dateTime) : IRequestHandler<GetWalletSummaryRequest, GetWalletSummaryResponse>
{
    public async Task<GetWalletSummaryResponse> Handle(GetWalletSummaryRequest request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(dateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var walletBalances = await dbContext.Wallets
            .Where(w => w.UserId == currentUser.UserId)
            .Select(w => w.InitialBalance
                + (w.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0)
                - (w.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0))
            .ToListAsync(ct);

        var monthlyIncome = await dbContext.Transactions
            .Where(t => t.UserId == currentUser.UserId && t.Type == TransactionType.Income && t.Date >= monthStart && t.Date < nextMonth)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var monthlyExpense = await dbContext.Transactions
            .Where(t => t.UserId == currentUser.UserId && t.Type == TransactionType.Expense && t.Date >= monthStart && t.Date < nextMonth)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var recentTransactions = await dbContext.Transactions
            .Where(t => t.UserId == currentUser.UserId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedOn)
            .Select(t => new RecentTransactionResponse(
                t.Id,
                t.Amount,
                t.Type,
                t.Date,
                t.Note,
                t.WalletId,
                t.Wallet!.Name,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null
            ))
            .Take(5)
            .ToListAsync(ct);

        return new GetWalletSummaryResponse(walletBalances.Sum(), monthlyIncome, monthlyExpense, recentTransactions);
    }
}
