using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Wallets;

public record DeleteWalletRequest(Guid Id);

public record DeleteWalletResponse(bool Success);

public class DeleteWalletEndpoint : IEndpoint
{
    public const string Route = "wallets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteWalletHandler handler, CancellationToken ct) => handler.Handle(new DeleteWalletRequest(id), ct))
        .WithTags("Wallets");
}

public class DeleteWalletHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<DeleteWalletRequest, DeleteWalletResponse>
{
    public async Task<DeleteWalletResponse> Handle(DeleteWalletRequest request, CancellationToken ct)
    {
        var wallet = await dbContext.Wallets.FindAsync([request.Id], ct);
        if (wallet is null || wallet.UserId != currentUser.UserId)
            throw new NotFoundException();

        dbContext.Wallets.Remove(wallet);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteWalletResponse(true);
    }
}
