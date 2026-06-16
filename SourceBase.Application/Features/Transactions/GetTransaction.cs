using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Transactions;

public record GetTransactionRequest(Guid Id);

public partial record TransactionResponse { }

public class GetTransactionEndpoint : IEndpoint
{
    public const string Route = "transactions/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTransactionRequest request, GetTransactionHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transactions");
}

public class GetTransactionHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTransactionRequest, TransactionResponse>
{
    public async Task<TransactionResponse> Handle(GetTransactionRequest request, CancellationToken ct)
    {
        var transaction = await dbContext.Transactions
            .Where(t => t.Id == request.Id && t.UserId == currentUser.UserId)
            .Select(t => new TransactionResponse(
                t.Id,
                t.Amount,
                t.Type,
                t.Date,
                t.Note,
                t.WalletId,
                t.Wallet!.Name,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null,
                t.IsTransfer
            ))
            .FirstOrDefaultAsync(ct);

        return transaction!;
    }
}

public class GetTransactionRequestValidator : AbstractValidator<GetTransactionRequest>
{
    public GetTransactionRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) => await dbContext.Transactions.AnyAsync(t => t.Id == id && t.UserId == currentUser.UserId, ct))
            .WithMessage("Transaction not found.");
    }
}
