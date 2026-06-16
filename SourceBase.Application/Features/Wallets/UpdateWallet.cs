using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Wallets;

public record UpdateWalletRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string? Name, string? Icon);

public record UpdateWalletResponse(Guid Id);

public class UpdateWalletEndpoint : IEndpoint
{
    public const string Route = "wallets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateWalletRequest body, UpdateWalletHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("Wallets");
}

public class UpdateWalletHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<UpdateWalletRequest, UpdateWalletResponse>
{
    public async Task<UpdateWalletResponse> Handle(UpdateWalletRequest request, CancellationToken ct)
    {
        var wallet = await dbContext.Wallets.FindAsync([request.Id], ct);
        if (wallet is null || wallet.UserId != currentUser.UserId)
            throw new NotFoundException();

        wallet.Name = request.Name ?? wallet.Name;
        wallet.Icon = request.Icon ?? wallet.Icon;
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(GetWalletSummaryHandler.CacheKey(currentUser.UserId), ct);
        return new UpdateWalletResponse(wallet.Id);
    }
}

public class UpdateWalletRequestValidator : AbstractValidator<UpdateWalletRequest>
{
    public UpdateWalletRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
    }
}
