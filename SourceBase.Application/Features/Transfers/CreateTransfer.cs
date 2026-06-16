using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;

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
    public CreateTransferRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.FromWalletId)
            .NotEmpty()
            .Must(x => x != Guid.Empty)
            .MustAsync(async (id, ct) =>
            {
                var wallet = await dbContext.Wallets.FindAsync([id], ct);
                return wallet is not null && wallet.UserId == currentUser.UserId;
            })
            .WithMessage("Source wallet not found.");

        RuleFor(x => x.ToWalletId)
            .NotEmpty()
            .Must(x => x != Guid.Empty)
            .MustAsync(async (id, ct) =>
            {
                var wallet = await dbContext.Wallets.FindAsync([id], ct);
                return wallet is not null && wallet.UserId == currentUser.UserId;
            })
            .WithMessage("Destination wallet not found.")
            .When(x => x.ToWalletId != Guid.Empty);

        RuleFor(x => x.ToWalletId)
            .NotEqual(x => x.FromWalletId)
            .WithMessage("Source and destination wallets must be different.")
            .When(x => x.FromWalletId != Guid.Empty && x.ToWalletId != Guid.Empty);

        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).NotNull();
    }
}
