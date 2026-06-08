using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Transfers;

public record DeleteTransferRequest(Guid Id);

public record DeleteTransferResponse(bool Success);

public class DeleteTransferEndpoint : IEndpoint
{
    public const string Route = "transfers/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteTransferHandler handler, CancellationToken ct) => handler.Handle(new DeleteTransferRequest(id), ct))
        .WithTags("Transfers");
}

public class DeleteTransferHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteTransferRequest, DeleteTransferResponse>
{
    public async Task<DeleteTransferResponse> Handle(DeleteTransferRequest request, CancellationToken ct)
    {
        var transfer = await dbContext.Transfers.FindAsync([request.Id], ct);
        if (transfer is null || transfer.UserId != currentUser.UserId)
            throw new NotFoundException();

        var transactions = await dbContext.Transactions
            .Where(t => t.Id == transfer.FromTransactionId || t.Id == transfer.ToTransactionId)
            .ToListAsync(ct);

        dbContext.Transactions.RemoveRange(transactions);
        dbContext.Transfers.Remove(transfer);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTransferResponse(true);
    }
}
