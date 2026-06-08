using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Wallets;

public record ConfigureWalletRequest([property: SwaggerIgnore] Guid Id, string Currency);

public record ConfigureWalletResponse(Guid Id);

public class ConfigureWalletEndpoint : IEndpoint
{
    public const string Route = "wallets/{id:guid}/config";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] ConfigureWalletRequest body, ConfigureWalletHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .WithTags("Wallets");
}

public class ConfigureWalletHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<ConfigureWalletRequest, ConfigureWalletResponse>
{
    public async Task<ConfigureWalletResponse> Handle(ConfigureWalletRequest request, CancellationToken ct)
    {
        var wallet = await dbContext.Wallets.FindAsync([request.Id], ct);
        if (wallet is null || wallet.UserId != currentUser.UserId)
            throw new NotFoundException();

        wallet.Currency = request.Currency.Trim().ToUpperInvariant();
        await dbContext.SaveChangesAsync(ct);
        return new ConfigureWalletResponse(wallet.Id);
    }
}

public class ConfigureWalletRequestValidator : AbstractValidator<ConfigureWalletRequest>
{
    public ConfigureWalletRequestValidator()
    {
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}
