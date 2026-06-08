using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Transfers;

public record CreateTransferRequest(Guid FromWalletId, Guid ToWalletId, decimal Amount, DateOnly? Date, string? Note);

public record CreateTransferResponse(Guid Id);

public class CreateTransferEndpoint : IEndpoint
{
    public const string Route = "transfers";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTransferRequest request, CreateTransferHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transfers");
}

public class CreateTransferHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateTransferRequest, CreateTransferResponse>
{
    public async Task<CreateTransferResponse> Handle(CreateTransferRequest request, CancellationToken ct)
    {
        if (request.FromWalletId == request.ToWalletId)
            throw new Shared.ValidationException("Source and destination wallets must be different.");

        var walletIds = await dbContext.Wallets
            .Where(w => w.UserId == currentUser.UserId && (w.Id == request.FromWalletId || w.Id == request.ToWalletId))
            .Select(w => w.Id)
            .ToListAsync(ct);

        if (!walletIds.Contains(request.FromWalletId))
            throw new NotFoundException("Source wallet not found.");
        if (!walletIds.Contains(request.ToWalletId))
            throw new NotFoundException("Destination wallet not found.");

        var fromTransaction = new TransactionEntity
        {
            WalletId = request.FromWalletId,
            Amount = request.Amount,
            Type = TransactionType.Expense,
            Date = request.Date!.Value,
            Note = request.Note,
            UserId = currentUser.UserId,
            IsTransfer = true,
        };

        var toTransaction = new TransactionEntity
        {
            WalletId = request.ToWalletId,
            Amount = request.Amount,
            Type = TransactionType.Income,
            Date = request.Date.Value,
            Note = request.Note,
            UserId = currentUser.UserId,
            IsTransfer = true,
        };

        dbContext.Transactions.AddRange(fromTransaction, toTransaction);
        await dbContext.SaveChangesAsync(ct);

        var transfer = new TransferEntity
        {
            FromWalletId = request.FromWalletId,
            ToWalletId = request.ToWalletId,
            Amount = request.Amount,
            Date = request.Date.Value,
            Note = request.Note,
            FromTransactionId = fromTransaction.Id,
            ToTransactionId = toTransaction.Id,
            UserId = currentUser.UserId,
        };

        dbContext.Transfers.Add(transfer);
        await dbContext.SaveChangesAsync(ct);
        return new CreateTransferResponse(transfer.Id);
    }
}

public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.FromWalletId).NotEmpty();
        RuleFor(x => x.ToWalletId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).NotNull();
    }
}
