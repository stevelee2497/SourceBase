using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SourceBase.Application.Features.GoldPrices;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.BackgroundServices;

public class GoldPriceScraperService(
    AppSettings appSettings,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IEnumerable<IGoldPriceScraper> scrapers,
    ILogger<GoldPriceScraperService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!appSettings.BackgroundJobSettings.Enabled)
        {
            logger.LogInformation("Background jobs are disabled. GoldPriceScraperService will not run.");
            return;
        }

        using var timer = new PeriodicTimer(appSettings.BackgroundJobSettings.GoldPriceScrapingInterval);
        do
        {
            await RunScrapersAsync(ct);
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RunScrapersAsync(CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient(Constants.HttpClientName);
        var goldPrices = new List<CreateGoldPriceItem>();
        foreach (var scraper in scrapers)
        {
            try
            {
                var html = await http.GetStringAsync(scraper.Url, ct);
                var result = await scraper.ParseAsync(html, ct);
                if (result is null)
                {
                    logger.LogWarning("Scraper returned no result for source {Source}", scraper.Source);
                    continue;
                }

                goldPrices.Add(new CreateGoldPriceItem(scraper.Source, result.Value.BuyPrice, result.Value.SellPrice, DateTime.UtcNow));

                logger.LogInformation("Scraped {Source}: buy={Buy} sell={Sell}", scraper.Source, result.Value.BuyPrice, result.Value.SellPrice);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Scraper failed for source {Source}", scraper.Source);
            }
        }
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateGoldPriceHandler>();
        await handler.Handle(new CreateGoldPriceRequest(goldPrices), ct);
    }
}
