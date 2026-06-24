using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using SourceBase.Web.Auth;
using SourceBase.Web.Services;
using StackExchange.Redis;
using System.IO.Compression;

namespace SourceBase.Web;

public static class ProgramConfigurations
{
    public static void AddSeriLog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
    }

    public static void AddCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(opts => opts.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(opts => opts.Level = CompressionLevel.Fastest);
    }

    public static void AddBlazorOptions(this IServiceCollection services)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.Configure<CircuitOptions>(options =>
        {
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
            options.DisconnectedCircuitMaxRetained = 200;
            options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
        });
    }

    public static void AddSignalROptions(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
            options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        });
    }

    public static void AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.Get<AppSettings>() ?? new AppSettings();
        services.AddSingleton(appSettings);

        if (appSettings.RedisEnabled)
        {
            var redisConnection = configuration.GetConnectionString("RedisConnection");
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                var redis = ConnectionMultiplexer.Connect(redisConnection);
                services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
                    .SetApplicationName("SourceBase.Web");
            }
        }

        services.AddHttpClient("api", client => client.BaseAddress = new Uri(appSettings.ApiBaseUrl));
        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));
    }

    public static void AddDependencyInjection(this IServiceCollection services)
    {
        services.AddScoped<BlazorAuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<BlazorAuthStateProvider>());
        services.AddScoped<ApiHttpClient>();
        services.AddScoped<ToastService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<UserTimeZoneService>();
    }

    public static void UseStaticFilesWithCache(this WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "public, max-age=3600";
            }
        });
    }
}
