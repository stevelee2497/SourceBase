using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Api.Features.Wallets;

public record UpdateWalletRequest([property: SwaggerIgnore] Guid Id, string Name, string? Icon);

public record UpdateWalletResponse(Guid Id);

public class UpdateWalletEndpoint : IEndpoint
{
    public const string Route = "wallets/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] UpdateWalletRequest body, UpdateWalletHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .WithTags("Wallets");
}

public class UpdateWalletHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateWalletRequest, UpdateWalletResponse>
{
    public async Task<UpdateWalletResponse> Handle(UpdateWalletRequest request, CancellationToken ct)
    {
        var wallet = await dbContext.Wallets.FindAsync([request.Id], ct);
        if (wallet is null || wallet.UserId != currentUser.UserId)
            throw new NotFoundException();

        wallet.Name = request.Name;
        wallet.Icon = request.Icon;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateWalletResponse(wallet.Id);
    }
}

public class UpdateWalletRequestValidator : AbstractValidator<UpdateWalletRequest>
{
    public UpdateWalletRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
