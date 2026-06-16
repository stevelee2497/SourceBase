using FluentValidation;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Wallets;

public record DeleteWalletRequest(Guid Id);

public record DeleteWalletResponse(bool Success);

public class DeleteWalletEndpoint : IEndpoint
{
    public const string Route = "wallets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteWalletRequest request, DeleteWalletHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Wallets");
}

public class DeleteWalletHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<DeleteWalletRequest, DeleteWalletResponse>
{
    public async Task<DeleteWalletResponse> Handle(DeleteWalletRequest request, CancellationToken ct)
    {
        var wallet = await dbContext.Wallets.FindAsync([request.Id], ct);
        dbContext.Wallets.Remove(wallet!);
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(GetWalletSummaryHandler.CacheKey(currentUser.UserId), ct);
        return new DeleteWalletResponse(true);
    }
}

public class DeleteWalletRequestValidator : AbstractValidator<DeleteWalletRequest>
{
    public DeleteWalletRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var wallet = await dbContext.Wallets.FindAsync([id], ct);
                return wallet is not null && wallet.UserId == currentUser.UserId;
            })
            .WithMessage("Wallet not found.");
    }
}
