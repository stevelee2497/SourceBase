using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Wallets;

public record ConfigureWalletRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string Currency);

public record ConfigureWalletResponse(Guid Id);

public class ConfigureWalletEndpoint : IEndpoint
{
    public const string Route = "wallets/{id:guid}/config";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, ([FromBody] ConfigureWalletRequest body, ConfigureWalletHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Wallets");
}

public class ConfigureWalletHandler(IDbContext dbContext) : IRequestHandler<ConfigureWalletRequest, ConfigureWalletResponse>
{
    public async Task<ConfigureWalletResponse> Handle(ConfigureWalletRequest request, CancellationToken ct)
    {
        var wallet = await dbContext.Wallets.FindAsync([request.Id], ct);
        wallet!.Currency = request.Currency.Trim().ToUpperInvariant();
        await dbContext.SaveChangesAsync(ct);
        return new ConfigureWalletResponse(wallet.Id);
    }
}

public class ConfigureWalletRequestValidator : AbstractValidator<ConfigureWalletRequest>
{
    public ConfigureWalletRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var wallet = await dbContext.Wallets.FindAsync([id], ct);
                return wallet is not null && wallet.UserId == currentUser.UserId;
            })
            .WithMessage("Wallet not found.");

        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}
