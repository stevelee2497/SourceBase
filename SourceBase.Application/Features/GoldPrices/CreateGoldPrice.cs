using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var ids = new List<Guid>();

        foreach (var item in request.Items!)
        {
            var dt = item.RecordedAt!.Value;
            var recordedAt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Kind);

            var existing = await dbContext.GoldPrices.FirstOrDefaultAsync(x => x.Source == item.Source!.Value && x.RecordedAt == recordedAt, ct);

            if (existing is not null)
            {
                existing.BuyPrice = item.BuyPrice!.Value;
                existing.SellPrice = item.SellPrice!.Value;
                ids.Add(existing.Id);
            }
            else
            {
                var entity = new GoldPriceEntity
                {
                    Source = item.Source!.Value,
                    BuyPrice = item.BuyPrice!.Value,
                    SellPrice = item.SellPrice!.Value,
                    RecordedAt = recordedAt,
                };
                dbContext.GoldPrices.Add(entity);
                ids.Add(entity.Id);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return new CreateGoldPriceResponse(ids);
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
