using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Transactions;

public record CreateTransactionRequest(Guid WalletId, decimal Amount, TransactionType? Type, DateOnly? Date, string? Note, Guid CategoryId);

public record CreateTransactionResponse(Guid Id);

public class CreateTransactionEndpoint : IEndpoint
{
    public const string Route = "transactions";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTransactionRequest request, CreateTransactionHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transactions");
}

public class CreateTransactionHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<CreateTransactionRequest, CreateTransactionResponse>
{
    public async Task<CreateTransactionResponse> Handle(CreateTransactionRequest request, CancellationToken ct)
    {
        var walletExists = await dbContext.Wallets.AnyAsync(w => w.Id == request.WalletId && w.UserId == currentUser.UserId, ct);
        if (!walletExists)
            throw new NotFoundException("Wallet not found.");

        var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId && (c.IsSystem || c.UserId == currentUser.UserId), ct);
        if (!categoryExists)
            throw new NotFoundException("Category not found.");

        var transaction = new TransactionEntity
        {
            WalletId = request.WalletId,
            Amount = request.Amount,
            Type = request.Type!.Value,
            Date = request.Date!.Value,
            Note = request.Note,
            CategoryId = request.CategoryId,
            UserId = currentUser.UserId,
            IsTransfer = false,
        };
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(GetWalletSummaryHandler.CacheKey(currentUser.UserId), ct);
        return new CreateTransactionResponse(transaction.Id);
    }
}

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.WalletId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Type).NotNull();
        RuleFor(x => x.Date).NotNull();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
