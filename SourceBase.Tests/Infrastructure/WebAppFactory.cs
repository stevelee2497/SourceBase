using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceBase.Api.Infrastructure.DbContexts;

namespace SourceBase.Tests.Infrastructure;

public class WebAppFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    // A unique name per factory instance keeps databases isolated between test classes
    private readonly string _dbName = $"sourcebase_test_{Guid.NewGuid():N}";
    private readonly string _connectionString;

    // Kept open for the lifetime of the factory so SQLite doesn't destroy the in-memory DB
    private SqliteConnection? _anchorConnection;

    public const string AdminEmail = "admin@test.com";
    public const string AdminPassword = "Test@1234!";

    public WebAppFactory()
    {
        _connectionString = $"Data Source={_dbName};Mode=Memory;Cache=Shared";
    }

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
                ["AppSettings:WebUrl"] = "http://localhost",
                ["AppSettings:SendGridApiKey"] = "test-key",
                ["AppSettings:SendGridAccountOwner"] = "test@test.com",
                ["Serilog:MinimumLevel"] = "Warning",
                ["Serilog:WriteTo:1:Name"] = "Console",
            });
        });
    }

    public async Task InitializeAsync()
    {
        // Open the anchor connection first so the named in-memory DB is never dropped
        _anchorConnection = new SqliteConnection(_connectionString);
        await _anchorConnection.OpenAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task<HttpClient> CreateAuthorizedClient()
    {
        var client = CreateClient();
        await client.AuthorizeAsync();
        return client;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync().AsTask();
        if (_anchorConnection != null)
            await _anchorConnection.DisposeAsync();
    }
}
