using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
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

public class GetWalletSummaryHandler(IDbContext dbContext, ICurrentUser currentUser, IDateTime dateTime, ICacheService cacheService) : IRequestHandler<GetWalletSummaryRequest, GetWalletSummaryResponse>
{
    public async Task<GetWalletSummaryResponse> Handle(GetWalletSummaryRequest request, CancellationToken ct)
    {
        var cached = await cacheService.GetAsync<GetWalletSummaryResponse>(CacheKeys.WalletSummary.WithId(currentUser.UserId), ct);
        if (cached is not null) return cached;

        var today = DateOnly.FromDateTime(dateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var walletBalances = await dbContext.Wallets
            .Where(w => w.UserId == currentUser.UserId)
            .Select(w => w.InitialBalance + w.Transactions.Sum(t => t.Amount * (t.Type == TransactionType.Income ? 1 : -1)))
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

        var result = new GetWalletSummaryResponse(walletBalances.Sum(), monthlyIncome, monthlyExpense, recentTransactions);
        await cacheService.SetAsync(CacheKeys.WalletSummary.WithId(currentUser.UserId), result, TimeSpan.FromMinutes(5), ct);
        return result;
    }
}
