using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Transactions;

public record GetTransactionSummaryRequest(Guid? WalletId, DateOnly? DateFrom, DateOnly? DateTo);

public record CategoryBreakdownResponse(Guid? CategoryId, string? CategoryName, TransactionType Type, decimal Total);

public record GetTransactionSummaryResponse(decimal TotalIncome, decimal TotalExpense, decimal NetBalance, List<CategoryBreakdownResponse> ByCategory);

public class GetTransactionSummaryEndpoint : IEndpoint
{
    public const string Route = "transactions/summary";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTransactionSummaryRequest request, GetTransactionSummaryHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transactions");
}

public class GetTransactionSummaryHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTransactionSummaryRequest, GetTransactionSummaryResponse>
{
    public async Task<GetTransactionSummaryResponse> Handle(GetTransactionSummaryRequest request, CancellationToken ct)
    {
        var transactions = dbContext.Transactions
            .Where(t => t.UserId == currentUser.UserId
                && !t.IsTransfer
                && (request.WalletId == null || t.WalletId == request.WalletId)
                && (request.DateFrom == null || t.Date >= request.DateFrom)
                && (request.DateTo == null || t.Date <= request.DateTo));

        var totalIncome = await transactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var totalExpense = await transactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var byCategory = await transactions
            .GroupBy(t => new { t.CategoryId, CategoryName = t.Category != null ? t.Category.Name : null, t.Type })
            .Select(g => new CategoryBreakdownResponse(g.Key.CategoryId, g.Key.CategoryName, g.Key.Type, g.Sum(t => t.Amount)))
            .ToListAsync(ct);

        return new GetTransactionSummaryResponse(totalIncome, totalExpense, totalIncome - totalExpense, byCategory);
    }
}
