using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.DbContexts;
using SourceBase.Infrastructure.Implementations;

namespace SourceBase.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>()!;
        var connectionString = configuration.GetConnectionString("DefaultConnection");
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
                options.BearerTokenExpiration = TimeSpan.FromMinutes(appSettings.AccessTokenExpirationMinutes);
                options.RefreshTokenExpiration = TimeSpan.FromMinutes(appSettings.RefreshTokenExpirationMinutes);
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
