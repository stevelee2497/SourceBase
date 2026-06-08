using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Transactions;

public record GetTransactionRequest(Guid Id);

public partial record TransactionResponse { }

public class GetTransactionEndpoint : IEndpoint
{
    public const string Route = "transactions/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (Guid id, GetTransactionHandler handler, CancellationToken ct) => handler.Handle(new GetTransactionRequest(id), ct))
        .WithTags("Transactions");
}

public class GetTransactionHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTransactionRequest, TransactionResponse>
{
    public async Task<TransactionResponse> Handle(GetTransactionRequest request, CancellationToken ct)
    {
        var transaction = await dbContext.Transactions
            .Where(t => t.Id == request.Id && t.UserId == currentUser.UserId)
            .Select(t => new TransactionResponse(
                t.Id,
                t.Amount,
                t.Type,
                t.Date,
                t.Note,
                t.WalletId,
                t.Wallet!.Name,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null,
                t.IsTransfer
            ))
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException();

        return transaction;
    }
}
