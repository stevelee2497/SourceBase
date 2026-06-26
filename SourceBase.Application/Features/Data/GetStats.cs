using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Data;

public record GetStatsRequest;

public record GetStatsResponse(int UserCount, decimal TotalBalance, decimal MonthlyIncome, decimal MonthlyExpense, bool AllLogged, string LogTimeDetail);

public class GetStatsEndpoint : IEndpoint
{
    public const string Route = "data/stats";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GetStatsHandler handler, CancellationToken ct) => handler.Handle(new GetStatsRequest(), ct))
        .WithTags("Data");
}

public class GetStatsHandler(IDbContext dbContext, ICurrentUser currentUser, IDateTime dateTime) : IRequestHandler<GetStatsRequest, GetStatsResponse>
{
    public async Task<GetStatsResponse> Handle(GetStatsRequest request, CancellationToken ct)
    {
        var userCount = await dbContext.Users.CountAsync(ct);

        var today = DateOnly.FromDateTime(dateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var walletBalance = await dbContext.Wallets
            .Where(w => w.UserId == currentUser.UserId)
            .Select(w => w.InitialBalance + w.Transactions.Sum(t => t.Amount * (t.Type == TransactionType.Income ? 1 : -1)))
            .SumAsync(ct);

        var monthlyIncome = await dbContext.Transactions
            .Where(t => t.UserId == currentUser.UserId && t.Type == TransactionType.Income && t.Date >= monthStart && t.Date < nextMonth)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var monthlyExpense = await dbContext.Transactions
            .Where(t => t.UserId == currentUser.UserId && t.Type == TransactionType.Expense && t.Date >= monthStart && t.Date < nextMonth)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var loggedDates = await dbContext.TimeSheets
            .Where(x => x.UserId == currentUser.UserId && x.Date.Year == today.Year && x.Date.Month == today.Month && x.Hours > 0)
            .Select(x => x.Date)
            .Distinct()
            .ToListAsync(ct);

        var (allLogged, logTimeDetail) = ComputeLogTimeStatus(today, loggedDates.ToHashSet());

        return new GetStatsResponse(userCount, walletBalance, monthlyIncome, monthlyExpense, allLogged, logTimeDetail);
    }

    private static (bool AllLogged, string LogTimeDetail) ComputeLogTimeStatus(DateOnly today, HashSet<DateOnly> loggedDates)
    {
        var monday = today.AddDays(-(((int)today.DayOfWeek - 1 + 7) % 7));
        var weekdaysSoFar = Enumerable.Range(0, today.DayNumber - monday.DayNumber + 1)
            .Select(i => monday.AddDays(i))
            .Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ToList();

        if (weekdaysSoFar.Count == 0) return (false, "No weekdays yet this week");

        var logged = weekdaysSoFar.Count(d => loggedDates.Contains(d));
        return (weekdaysSoFar.All(d => loggedDates.Contains(d)), $"{logged}/{weekdaysSoFar.Count} days this week");
    }
}
