using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Transactions;

public record GetTransactionsRequest(Guid? WalletId, TransactionType? Type, Guid? CategoryId, DateOnly? DateFrom, DateOnly? DateTo, int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Desc) : PagingRequest(Page, Limit, Order, null);

public partial record TransactionResponse(Guid Id, decimal Amount, TransactionType Type, DateOnly Date, string? Note, Guid WalletId, string WalletName, Guid? CategoryId, string? CategoryName, bool IsTransfer);

public class GetTransactionsEndpoint : IEndpoint
{
    public const string Route = "transactions";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTransactionsRequest request, GetTransactionsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transactions");
}

public class GetTransactionsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTransactionsRequest, PagingResponse<TransactionResponse>>
{
    public async Task<PagingResponse<TransactionResponse>> Handle(GetTransactionsRequest request, CancellationToken ct)
    {
        return await dbContext.Transactions
            .Where(t => t.UserId == currentUser.UserId
                && (request.WalletId == null || t.WalletId == request.WalletId)
                && (request.Type == null || t.Type == request.Type)
                && (request.CategoryId == null || t.CategoryId == request.CategoryId)
                && (request.DateFrom == null || t.Date >= request.DateFrom)
                && (request.DateTo == null || t.Date <= request.DateTo))
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedOn)
            .PaginateAsync(t => new TransactionResponse(
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
            ), request, ct);
    }
}
