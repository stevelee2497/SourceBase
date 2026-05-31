using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Wallets;

public record GetWalletsRequest();

public partial record WalletResponse(Guid Id, string Name, decimal Balance, decimal InitialBalance, string Currency, string? Icon);

public record GetWalletsResponse(List<WalletResponse> Wallets, decimal TotalBalance);

public class GetWalletsEndpoint : IEndpoint
{
    public const string Route = "wallets";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetWalletsRequest request, GetWalletsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Wallets");
}

public class GetWalletsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetWalletsRequest, GetWalletsResponse>
{
    public async Task<GetWalletsResponse> Handle(GetWalletsRequest request, CancellationToken ct)
    {
        var wallets = await dbContext.Wallets
            .Where(w => w.UserId == currentUser.UserId)
            .Select(w => new WalletResponse(
                w.Id,
                w.Name,
                w.InitialBalance
                    + (w.Transactions.Where(t => t.Type == SourceBase.Api.Entities.TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0)
                    - (w.Transactions.Where(t => t.Type == SourceBase.Api.Entities.TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0),
                w.InitialBalance,
                w.Currency,
                w.Icon
            ))
            .ToListAsync(ct);

        return new GetWalletsResponse(wallets, wallets.Sum(w => w.Balance));
    }
}
