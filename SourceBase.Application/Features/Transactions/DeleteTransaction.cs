using FluentValidation;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Transactions;

public record DeleteTransactionRequest(Guid Id);

public record DeleteTransactionResponse(bool Success);

public class DeleteTransactionEndpoint : IEndpoint
{
    public const string Route = "transactions/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteTransactionRequest request, DeleteTransactionHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transactions");
}

public class DeleteTransactionHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<DeleteTransactionRequest, DeleteTransactionResponse>
{
    public async Task<DeleteTransactionResponse> Handle(DeleteTransactionRequest request, CancellationToken ct)
    {
        var transaction = await dbContext.Transactions.FindAsync([request.Id], ct);
        dbContext.Transactions.Remove(transaction!);
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(GetWalletSummaryHandler.CacheKey(currentUser.UserId), ct);
        return new DeleteTransactionResponse(true);
    }
}

public class DeleteTransactionRequestValidator : AbstractValidator<DeleteTransactionRequest>
{
    public DeleteTransactionRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var txn = await dbContext.Transactions.FindAsync([id], ct);
                return txn is not null && txn.UserId == currentUser.UserId;
            })
            .WithMessage("Transaction not found.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var txn = await dbContext.Transactions.FindAsync([id], ct);
                        return txn is not { IsTransfer: true };
                    })
                    .WithMessage("Transfer transactions cannot be deleted directly. Delete the transfer instead.");
            });
    }
}
