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
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Infrastructure.DbContexts;
using Xunit;


namespace SourceBase.Tests.Infrastructure;

public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.com";
    public const string AdminPassword = "Test@1234!_Aokfn1";

    public FakeDateTimeProvider FakeDateTime { get; } = new();

    private SqliteConnection? _anchorConnection;
    private readonly string _connectionString = $"Data Source=test_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AdminEmail"] = AdminEmail,
                ["AppSettings:AdminPassword"] = AdminPassword,
                ["AppSettings:Roles:0"] = "Admin",
                ["AppSettings:Roles:1"] = "User",
                ["AppSettings:OtpTokenExpiration"] = "00:15:00",
                ["AppSettings:AccessTokenExpiration"] = "01:00:00",
                ["AppSettings:RefreshTokenExpiration"] = "14.00:00:00",
                ["AppSettings:SendGridApiKey"] = "", // Disable real email sending during tests
                ["ConnectionStrings:RedisConnection"] = "", // Disable Redis — RedisCacheService no-ops when connection string is empty
                ["Serilog:MinimumLevel:Default"] = "Fatal",
                ["Logging:LogLevel:Default"] = "Fatal",
            });
        });

        builder.ConfigureServices((ctx, services) =>
        {
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });

            // Replace the production PostgreSQL DbContext with SQLite in-memory for tests
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            // Replace real DateTimeProvider with controllable fake
            services.RemoveAll<IDateTime>();
            services.AddSingleton<IDateTime>(FakeDateTime);


            var appConfig = ctx.Configuration;
            services.AddDbContext<ApplicationDbContext>((_, options) =>
            {
                options.UseSqlite(_connectionString)
                    .UseSeeding((context, _) => ApplicationDbContext.SeedData(context, appConfig))
                    .UseAsyncSeeding(async (context, _, _) => ApplicationDbContext.SeedData(context, appConfig));
            });
        });

        ClientOptions.BaseAddress = new Uri("http://localhost/api/");
    }

    public async Task InitializeAsync()
    {
        _anchorConnection = new SqliteConnection(_connectionString);
        await _anchorConnection.OpenAsync();

        await WithDbContextAsync(db => db.Database.EnsureCreatedAsync());
    }

    public new async Task DisposeAsync()
    {
        if (_anchorConnection != null)
            await _anchorConnection.DisposeAsync();
        await base.DisposeAsync().AsTask();
    }

    public async Task<HttpClient> CreateAuthorizedClient(string email = AdminEmail, string password = AdminPassword)
    {
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
