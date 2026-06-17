using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.GoldPrices;

public record CreateGoldPriceItem(GoldSource? Source, decimal? BuyPrice, decimal? SellPrice, DateTime? RecordedAt);

public record CreateGoldPriceRequest(List<CreateGoldPriceItem>? Items);

public record CreateGoldPriceResponse(List<Guid> Ids);

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
        var entities = request.Items!.Select(item => new GoldPriceEntity
        {
            Source = item.Source!.Value,
            BuyPrice = item.BuyPrice!.Value,
            SellPrice = item.SellPrice!.Value,
            RecordedAt = item.RecordedAt!.Value,
        }).ToList();

        dbContext.GoldPrices.AddRange(entities);
        await dbContext.SaveChangesAsync(ct);
        return new CreateGoldPriceResponse(entities.Select(e => e.Id).ToList());
    }
}

public class CreateGoldPriceRequestValidator : AbstractValidator<CreateGoldPriceRequest>
{
    public CreateGoldPriceRequestValidator()
    {
        RuleFor(x => x.Items).NotNull().NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Source).NotNull().IsInEnum();
            item.RuleFor(x => x.BuyPrice).NotNull().GreaterThan(0);
            item.RuleFor(x => x.SellPrice).NotNull().GreaterThan(0);
            item.RuleFor(x => x.RecordedAt).NotNull();
        });
    }
}
