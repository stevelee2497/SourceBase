using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
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
        var transaction = await dbContext.Transactions.FindAsync([request.Id], ct);
        if (transaction is null || transaction.UserId != currentUser.UserId)
            throw new NotFoundException();
        if (transaction.IsTransfer)
            throw new Shared.ValidationException("Transfer transactions cannot be edited directly. Delete the transfer instead.");

        if (request.WalletId is not null)
        {
            var walletExists = await dbContext.Wallets.AnyAsync(w => w.Id == request.WalletId.Value && w.UserId == currentUser.UserId, ct);
            if (!walletExists) throw new BadRequestException("Wallet not found.");
        }

        if (request.CategoryId is not null)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && (c.IsSystem || c.UserId == currentUser.UserId), ct);
            if (!categoryExists) throw new BadRequestException("Category not found.");
        }

        transaction.WalletId = request.WalletId ?? transaction.WalletId;
        transaction.Amount = request.Amount ?? transaction.Amount;
        transaction.Type = request.Type ?? transaction.Type;
        transaction.Date = request.Date ?? transaction.Date;
        transaction.Note = request.Note ?? transaction.Note;
        transaction.CategoryId = request.CategoryId ?? transaction.CategoryId;
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.WalletSummary.WithId(currentUser.UserId), ct);
        return new UpdateTransactionResponse(transaction.Id);
    }
}

public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount is not null);
    }
}
