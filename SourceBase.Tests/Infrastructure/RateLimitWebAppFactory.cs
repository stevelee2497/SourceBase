using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SourceBase.Tests.Infrastructure;

/// <summary>
/// A test factory with very low rate limits so tests can trigger 429s without making many requests.
/// </summary>
public class RateLimitWebAppFactory : WebAppFactory
{
    public const int StrictPermitLimit = 3;
    public const int GeneralPermitLimit = 50;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimitSettings:StrictPermitLimit"] = StrictPermitLimit.ToString(),
                ["RateLimitSettings:GeneralPermitLimit"] = GeneralPermitLimit.ToString(),
            });
        });
    }
}
