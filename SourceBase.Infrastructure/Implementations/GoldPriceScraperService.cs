using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations;

public class GoldPriceScraperService(
    IServiceScopeFactory scopeFactory,
    IEnumerable<IGoldPriceScraper> scrapers,
    ILogger<GoldPriceScraperService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            await RunScrapersAsync(ct);
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RunScrapersAsync(CancellationToken ct)
    {
        foreach (var scraper in scrapers)
        {
            try
            {
                var result = await scraper.ScrapeAsync(ct);
                if (result is null)
                {
                    logger.LogWarning("Scraper returned no result for source {Source}", scraper.Source);
                    continue;
                }

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
                db.GoldPrices.Add(new GoldPriceEntity
                {
                    Source = scraper.Source,
                    BuyPrice = result.Value.BuyPrice,
                    SellPrice = result.Value.SellPrice,
                    RecordedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync(ct);

                logger.LogInformation("Scraped {Source}: buy={Buy} sell={Sell}", scraper.Source, result.Value.BuyPrice, result.Value.SellPrice);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Scraper failed for source {Source}", scraper.Source);
            }
        }
    }
}
