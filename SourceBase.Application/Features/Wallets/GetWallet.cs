using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Wallets;

public record GetWalletRequest(Guid Id);

public partial record WalletResponse { }

public class GetWalletEndpoint : IEndpoint
{
    public const string Route = "wallets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetWalletRequest request, GetWalletHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Wallets");
}

public class GetWalletHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetWalletRequest, WalletResponse>
{
    public async Task<WalletResponse> Handle(GetWalletRequest request, CancellationToken ct)
    {
        var wallet = await dbContext.Wallets
            .Where(w => w.Id == request.Id && w.UserId == currentUser.UserId)
            .Select(w => new WalletResponse(
                w.Id,
                w.Name,
                w.InitialBalance + w.Transactions.Sum(t => t.Amount * (t.Type == TransactionType.Income ? 1 : -1)),
                w.InitialBalance,
                w.Currency,
                w.Icon
            ))
            .FirstOrDefaultAsync(ct);

        return wallet!;
    }
}

public class GetWalletRequestValidator : AbstractValidator<GetWalletRequest>
{
    public GetWalletRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) => await dbContext.Wallets.AnyAsync(w => w.Id == id && w.UserId == currentUser.UserId, ct))
            .WithMessage("Wallet not found.");
    }
}
