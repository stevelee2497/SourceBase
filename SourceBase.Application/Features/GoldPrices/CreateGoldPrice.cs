using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.GoldPrices;

public record CreateGoldPriceRequest(GoldSource? Source, decimal? BuyPrice, decimal? SellPrice, DateTime? RecordedAt);

public record CreateGoldPriceResponse(Guid Id);

public class CreateGoldPriceEndpoint : IEndpoint
{
    public const string Route = "gold-prices";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateGoldPriceRequest request, CreateGoldPriceHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("GoldPrices");
}

public class CreateGoldPriceHandler(IDbContext dbContext) : IRequestHandler<CreateGoldPriceRequest, CreateGoldPriceResponse>
{
    public async Task<CreateGoldPriceResponse> Handle(CreateGoldPriceRequest request, CancellationToken ct)
    {
        var entity = new GoldPriceEntity
        {
            Source = request.Source!.Value,
            BuyPrice = request.BuyPrice!.Value,
            SellPrice = request.SellPrice!.Value,
            RecordedAt = request.RecordedAt!.Value,
        };
        dbContext.GoldPrices.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return new CreateGoldPriceResponse(entity.Id);
    }
}

public class CreateGoldPriceRequestValidator : AbstractValidator<CreateGoldPriceRequest>
{
    public CreateGoldPriceRequestValidator()
    {
        RuleFor(x => x.Source).NotNull().IsInEnum();
        RuleFor(x => x.BuyPrice).NotNull().GreaterThan(0);
        RuleFor(x => x.SellPrice).NotNull().GreaterThan(0);
        RuleFor(x => x.RecordedAt).NotNull();
    }
}
