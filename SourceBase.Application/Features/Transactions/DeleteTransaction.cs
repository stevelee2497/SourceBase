using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Transactions;

public record DeleteTransactionRequest(Guid Id);

public record DeleteTransactionResponse(bool Success);

public class DeleteTransactionEndpoint : IEndpoint
{
    public const string Route = "transactions/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteTransactionHandler handler, CancellationToken ct) => handler.Handle(new DeleteTransactionRequest(id), ct))
        .WithTags("Transactions");
}

public class DeleteTransactionHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteTransactionRequest, DeleteTransactionResponse>
{
    public async Task<DeleteTransactionResponse> Handle(DeleteTransactionRequest request, CancellationToken ct)
    {
        var transaction = await dbContext.Transactions.FindAsync([request.Id], ct);
        if (transaction is null || transaction.UserId != currentUser.UserId)
            throw new NotFoundException();
        if (transaction.IsTransfer)
            throw new ValidationException("Transfer transactions cannot be deleted directly. Delete the transfer instead.");

        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTransactionResponse(true);
    }
}
