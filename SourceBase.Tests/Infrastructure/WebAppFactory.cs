using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceBase.Api.Features.Auth;
using SourceBase.Api.Infrastructure.DbContexts;
using Xunit;

namespace SourceBase.Tests.Infrastructure;

public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.com";
    public const string AdminPassword = "Test@1234!";

    private SqliteConnection? _anchorConnection;
    private readonly string _connectionString = $"Data Source=test_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["AppSettings:AdminEmail"] = AdminEmail,
                ["AppSettings:AdminPassword"] = AdminPassword,
                ["AppSettings:Roles:0"] = "Admin",
                ["AppSettings:Roles:1"] = "User",
                ["Serilog:MinimumLevel"] = "Error",
                ["AppSettings:WebUrl"] = "http://localhost",
            });
            builder.ConfigureServices(services =>
            {
                services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
                {
                    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                });
            });
        });

    }

    public async Task InitializeAsync()
    {
        _anchorConnection = new SqliteConnection(_connectionString);
        await _anchorConnection.OpenAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_anchorConnection != null)
            await _anchorConnection.DisposeAsync();
        await base.DisposeAsync().AsTask();
    }

    public async Task<HttpClient> CreateAuthorizedClient()
    {
        var client = CreateClient();
        var token = await GetAccessTokenAsync(client, AdminEmail, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<string> GetOtpCode(string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var otp = await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.OtpCode)
            .FirstOrDefaultAsync();
        return otp ?? throw new InvalidOperationException($"No OTP code found for '{email}'.");
    }

    public async Task<string> GetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body?.AccessToken ?? throw new InvalidOperationException("Access token not found in login response");
    }
}
