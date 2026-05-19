using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        await client.AuthorizeAsync();
        return client;
    }

    public async Task<string> GetLatestEmailCodeAsync(string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var otp = await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.OtpCode)
            .FirstOrDefaultAsync();
        return otp ?? throw new InvalidOperationException($"No OTP code found for '{email}'.");
    }
}
