using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Transfers;

public record GetTransfersRequest(Guid? WalletId, DateOnly? DateFrom, DateOnly? DateTo, int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Desc) : PagingRequest(Page, Limit, Order, null);

public record TransferResponse(Guid Id, Guid FromWalletId, string FromWalletName, Guid ToWalletId, string ToWalletName, decimal Amount, DateOnly Date, string? Note);

public class GetTransfersEndpoint : IEndpoint
{
    public const string Route = "transfers";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTransfersRequest request, GetTransfersHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Transfers");
}

public class GetTransfersHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTransfersRequest, PagingResponse<TransferResponse>>
{
    public async Task<PagingResponse<TransferResponse>> Handle(GetTransfersRequest request, CancellationToken ct)
    {
        return await dbContext.Transfers
            .Where(t => t.UserId == currentUser.UserId
                && (request.WalletId == null || t.FromWalletId == request.WalletId || t.ToWalletId == request.WalletId)
                && (request.DateFrom == null || t.Date >= request.DateFrom)
                && (request.DateTo == null || t.Date <= request.DateTo))
            .OrderByDescending(t => t.Date)
            .PaginateAsync(t => new TransferResponse(
                t.Id,
                t.FromWalletId,
                t.FromWallet!.Name,
                t.ToWalletId,
                t.ToWallet!.Name,
                t.Amount,
                t.Date,
                t.Note
            ), request, ct);
    }
}
