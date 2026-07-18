using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Infrastructure.BackgroundServices;
using SourceBase.Infrastructure.Implementations;
using SourceBase.Infrastructure.DbContexts;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;


namespace SourceBase.Tests.Infrastructure;

public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly string AdminEmail = "admin-qt@yopmail.com";
    public static readonly string AdminPassword = $"pw_{Guid.NewGuid():N}";

    public FakeDateTimeProvider FakeDateTime { get; } = new();

    private static readonly bool UsePostgres = string.Equals(Environment.GetEnvironmentVariable("USE_POSTGRES"), "true", StringComparison.OrdinalIgnoreCase);
    private static readonly bool UseRedis = string.Equals(Environment.GetEnvironmentVariable("USE_REDIS"), "true", StringComparison.OrdinalIgnoreCase);

    // SQLite-only fields
    private SqliteConnection? anchorConnection;
    private readonly string sqliteConnectionString = $"Data Source=test_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // PostgreSQL-only field
    private PostgreSqlContainer? postgresContainer;

    // Redis-only field
    private RedisContainer? redisContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(AppContext.BaseDirectory);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["AdminEmail"] = AdminEmail,
                ["AdminPassword"] = AdminPassword,
            };

            if (UseRedis && redisContainer != null)
            {
                inMemory["ConnectionStrings:RedisConnection"] = redisContainer.GetConnectionString();
                inMemory["RedisEnabled"] = "true";
            }

            config.AddInMemoryCollection(inMemory);
        });

        builder.ConfigureServices((ctx, services) =>
        {
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });

            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            // Replace real DateTimeProvider with controllable fake
            services.RemoveAll<IDateTime>();
            services.AddSingleton<IDateTime>(FakeDateTime);

            // Set up memory cache for local tests
            if (!UseRedis)
            {
                services.RemoveAll<ICacheService>();
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }

            // Remove all background services that could interfere with tests
            services.RemoveAll<BackgroundService>();
            services.RemoveAll<GoldPriceScraperService>();

            // Prevent background service failures from stopping the test host
            services.Configure<HostOptions>(o => o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

            var appConfig = ctx.Configuration;

            if (UsePostgres)
            {
                var connectionString = postgresContainer!.GetConnectionString();
                services.AddDbContext<ApplicationDbContext>((_, options) =>
                {
                    options.UseNpgsql(connectionString, o => o.MigrationsAssembly("SourceBase.Infrastructure"))
                        .UseSeeding((context, _) => ApplicationDbContext.SeedData(context, appConfig))
                        .UseAsyncSeeding(async (context, _, _) => ApplicationDbContext.SeedData(context, appConfig));
                });
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>((_, options) =>
                {
                    options.UseSqlite(sqliteConnectionString)
                        .UseSeeding((context, _) => ApplicationDbContext.SeedData(context, appConfig))
                        .UseAsyncSeeding(async (context, _, _) => ApplicationDbContext.SeedData(context, appConfig));
                });
            }
        });

        ClientOptions.BaseAddress = new Uri("http://localhost/api/");
    }

    public async Task InitializeAsync()
    {
        if (UseRedis)
        {
            redisContainer = new RedisBuilder("redis:7-alpine").Build();
            await redisContainer.StartAsync();
        }

        if (UsePostgres)
        {
            postgresContainer = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await postgresContainer.StartAsync();
            await WithDbContextAsync(async db => { await db.Database.MigrateAsync(); return true; });
        }
        else
        {
            anchorConnection = new SqliteConnection(sqliteConnectionString);
            await anchorConnection.OpenAsync();
            await WithDbContextAsync(db => db.Database.EnsureCreatedAsync());
        }
    }

    public new async Task DisposeAsync()
    {

        if (postgresContainer != null)
            await postgresContainer.DisposeAsync();

        if (anchorConnection != null)
            await anchorConnection.DisposeAsync();

        if (redisContainer != null)
            await redisContainer.DisposeAsync();

        await base.DisposeAsync().AsTask();
    }

    private int _clientCounter;

    public new HttpClient CreateClient()
    {
        var client = base.CreateClient();
        // Give each test client a unique IP so rate limit buckets are isolated per client
        var n = Interlocked.Increment(ref _clientCounter);
        var ip = $"10.{(n >> 16) & 0xFF}.{(n >> 8) & 0xFF}.{n & 0xFF}";
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
        return client;
    }

    public async Task<HttpClient> CreateAuthorizedClient(string? email = null, string? password = null)
    {
        email ??= AdminEmail;
        password ??= AdminPassword;
        var client = CreateClient();

        if (email != AdminEmail)
        {
            await client.PostAsJsonAsync(RegisterEndpoint.Route, new
            {
                userName = $"user_{Guid.NewGuid():N}",
                email,
                password,
            });
            await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
            {
                email,
                code = await GetOtpCode(email),
            });
        }

        var token = await GetAccessTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<string> GetOtpCode(string email)
    {
        var otp = await WithDbContextAsync(async db => await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.OtpCode)
            .FirstOrDefaultAsync());
        return otp ?? throw new InvalidOperationException($"No OTP code found for '{email}'.");
    }

    public async Task<TResult> WithDbContextAsync<TResult>(Func<ApplicationDbContext, Task<TResult>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(db);
    }

    public async Task<string> GetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new { email, password });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body?.AccessToken ?? throw new InvalidOperationException("Access token not found in login response");
    }
}
