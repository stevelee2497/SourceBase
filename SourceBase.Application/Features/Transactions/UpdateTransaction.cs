using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Transactions;

public record UpdateTransactionRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, Guid? WalletId, decimal? Amount, TransactionType? Type, DateOnly? Date, string? Note, Guid? CategoryId);

public record UpdateTransactionResponse(Guid Id);

public class UpdateTransactionEndpoint : IEndpoint
{
    public const string Route = "transactions/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateTransactionRequest body, UpdateTransactionHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Transactions");
}

public class UpdateTransactionHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<UpdateTransactionRequest, UpdateTransactionResponse>
{
    public async Task<UpdateTransactionResponse> Handle(UpdateTransactionRequest request, CancellationToken ct)
    {
        var transaction = await dbContext.Transactions.FindAsync([request.Id], ct)!;

        transaction!.WalletId = request.WalletId ?? transaction.WalletId;
        transaction.Amount = request.Amount ?? transaction.Amount;
        transaction.Type = request.Type ?? transaction.Type;
        transaction.Date = request.Date ?? transaction.Date;
        transaction.Note = request.Note ?? transaction.Note;
        transaction.CategoryId = request.CategoryId ?? transaction.CategoryId;
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(GetWalletSummaryHandler.CacheKey(currentUser.UserId), ct);
        return new UpdateTransactionResponse(transaction.Id);
    }
}

public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var transaction = await dbContext.Transactions.FindAsync([id], ct);
                return transaction is not null && transaction.UserId == currentUser.UserId;
            })
            .WithMessage("Transaction not found.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var transaction = await dbContext.Transactions.FindAsync([id], ct);
                        return !transaction!.IsTransfer;
                    })
                    .WithMessage("Transfer transactions cannot be edited directly. Delete the transfer instead.");
            });

        RuleFor(x => x.WalletId)
            .MustAsync(async (id, ct) =>
            {
                var wallet = await dbContext.Wallets.FindAsync([id!.Value], ct);
                return wallet is not null && wallet.UserId == currentUser.UserId;
            })
            .WithMessage("Wallet not found.")
            .When(x => x.WalletId is not null);

        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount is not null);

        RuleFor(x => x.CategoryId)
            .MustAsync(async (id, ct) =>
            {
                var category = await dbContext.Categories.FindAsync([id!.Value], ct);
                return category is not null && (category.IsSystem || category.UserId == currentUser.UserId);
            })
            .WithMessage("Category not found.")
            .When(x => x.CategoryId is not null);
    }
}
