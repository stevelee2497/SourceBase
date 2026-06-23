using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Wallets;

public record CreateWalletRequest(string Name, decimal InitialBalance, string Currency, string? Icon);

public record CreateWalletResponse(Guid Id);

public class CreateWalletEndpoint : IEndpoint
{
    public const string Route = "wallets";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateWalletRequest request, CreateWalletHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Wallets");
}

public class CreateWalletHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<CreateWalletRequest, CreateWalletResponse>
{
    public async Task<CreateWalletResponse> Handle(CreateWalletRequest request, CancellationToken ct)
    {
        var wallet = new WalletEntity
        {
            Name = request.Name,
            InitialBalance = request.InitialBalance,
            Currency = request.Currency,
            Icon = request.Icon,
            UserId = currentUser.UserId,
        };
        dbContext.Wallets.Add(wallet);
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.WalletSummary.WithId(currentUser.UserId), ct);
        return new CreateWalletResponse(wallet.Id);
    }
}

public class CreateWalletRequestValidator : AbstractValidator<CreateWalletRequest>
{
    public CreateWalletRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Currency).NotEmpty();
    }
}
