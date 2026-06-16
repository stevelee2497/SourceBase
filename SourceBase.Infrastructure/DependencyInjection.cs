using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.DbContexts;
using SourceBase.Infrastructure.Implementations;
using StackExchange.Redis;

namespace SourceBase.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>()!;
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var redisConnection = configuration.GetConnectionString("RedisConnection");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            var redis = ConnectionMultiplexer.Connect(redisConnection);
            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
                .SetApplicationName("SourceBase.Api");
        }
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, o => o.MigrationsAssembly("SourceBase.Infrastructure"))
                .UseSeeding((context, _) => ApplicationDbContext.SeedData(context, configuration))
                .UseAsyncSeeding(async (context, _, _) => ApplicationDbContext.SeedData(context, configuration));
        });
        services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Constants.BearerScheme;
                options.DefaultChallengeScheme = Constants.BearerScheme;
                options.DefaultForbidScheme = Constants.BearerScheme;
            })
            .AddBearerToken(Constants.BearerScheme, options =>
            {
                options.BearerTokenExpiration = appSettings.AccessTokenExpiration;
                options.RefreshTokenExpiration = appSettings.RefreshTokenExpiration;
                options.Events = new BearerTokenEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddSignalR();
        services.AddSingleton<IDateTime, DateTimeProvider>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddScoped<ISecurityProvider, SecurityProvider>();
        services.AddScoped<IDbContext, ApplicationDbContext>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IEmailHelper, SendGridEmailHelper>();
        services.AddScoped<IStorageService, CloudflareR2StorageService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOtpHelper, OtpHelper>();
    }

    public static void EnsureDatabaseMigrated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
}
