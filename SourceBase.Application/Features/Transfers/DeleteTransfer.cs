using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Transfers;

public record DeleteTransferRequest(Guid Id);

public record DeleteTransferResponse(bool Success);

public class DeleteTransferEndpoint : IEndpoint
{
    public const string Route = "transfers/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteTransferRequest request, DeleteTransferHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transfers");
}

public class DeleteTransferHandler(IDbContext dbContext) : IRequestHandler<DeleteTransferRequest, DeleteTransferResponse>
{
    public async Task<DeleteTransferResponse> Handle(DeleteTransferRequest request, CancellationToken ct)
    {
        var transfer = await dbContext.Transfers.FindAsync([request.Id], ct);

        var transactions = await dbContext.Transactions
            .Where(t => t.Id == transfer!.FromTransactionId || t.Id == transfer.ToTransactionId)
            .ToListAsync(ct);

        dbContext.Transactions.RemoveRange(transactions);
        dbContext.Transfers.Remove(transfer!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteTransferResponse(true);
    }
}

public class DeleteTransferRequestValidator : AbstractValidator<DeleteTransferRequest>
{
    public DeleteTransferRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var transfer = await dbContext.Transfers.FindAsync([id], ct);
                return transfer is not null && transfer.UserId == currentUser.UserId;
            })
            .WithMessage("Transfer not found.");
    }
}
