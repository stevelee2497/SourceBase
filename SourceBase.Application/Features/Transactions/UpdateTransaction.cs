using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Wallets;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Transactions;

public record UpdateTransactionRequest([property: SwaggerIgnore] Guid Id, decimal Amount, TransactionType Type, DateOnly? Date, string? Note, Guid CategoryId);

public record UpdateTransactionResponse(Guid Id);

public class UpdateTransactionEndpoint : IEndpoint
{
    public const string Route = "transactions/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] UpdateTransactionRequest body, UpdateTransactionHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
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

        var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId && (c.IsSystem || c.UserId == currentUser.UserId), ct);
        if (!categoryExists)
            throw new NotFoundException("Category not found.");

        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
        transaction.Date = request.Date!.Value;
        transaction.Note = request.Note;
        transaction.CategoryId = request.CategoryId;
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(GetWalletSummaryHandler.CacheKey(currentUser.UserId), ct);
        return new UpdateTransactionResponse(transaction.Id);
    }
}

public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).NotNull();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
