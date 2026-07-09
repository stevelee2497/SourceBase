using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Google Disconnect",
    Route = "DELETE /api/auth/google/disconnect",
    Auth = "Authorized",
    UseCase = "As an authenticated user with Google linked, I want to unlink my Google account.",
    Description = new[]
    {
        "DELETE /api/auth/google/disconnect sets GoogleId = null on the current user.",
        "Requires the user to have a PasswordHash — cannot disconnect if no password is set.",
        "Returns 200 { success: true } on success.",
        "Anonymous access returns 401.",
        "Google-only account (no password) returns 400.",
        "Calling disconnect without a GoogleId linked is idempotent — returns 200.",
        "After disconnect, the user can still log in with email/password.",
        "Duplicate GoogleId across users is prevented by a DB unique index.",
    })]
public class GoogleDisconnectTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "GOOGLE-DISCONNECT-001: anonymous access returns 401")]
    public async Task Disconnect_Anonymous_Returns401()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DisconnectGoogleEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GOOGLE-DISCONNECT-002: user with password can disconnect Google — GoogleId removed from DB")]
    public async Task Disconnect_UserWithPassword_RemovesGoogleId()
    {
        // Arrange
        var email = $"disc_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");
        await factory.WithDbContextAsync(async db =>
        {
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.GoogleId = $"gid_{Guid.NewGuid():N}";
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await client.DeleteAsync(DisconnectGoogleEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var googleId = await factory.WithDbContextAsync(async db =>
            await db.Users.Where(u => u.Email == email).Select(u => u.GoogleId).FirstOrDefaultAsync());
        googleId.ShouldBeNull();
    }

    [Fact(DisplayName = "GOOGLE-DISCONNECT-003: disconnect response body contains success:true")]
    public async Task Disconnect_UserWithPassword_ResponseBodyHasSuccessTrue()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DisconnectGoogleEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DisconnectGoogleResponse>();
        body.ShouldNotBeNull();
        body!.Success.ShouldBeTrue();
    }

    [Fact(DisplayName = "GOOGLE-DISCONNECT-004: Google-only user (no password) cannot disconnect — returns 400")]
    public async Task Disconnect_GoogleOnlyUser_Returns400()
    {
        // Arrange — seed Google-only user then obtain a token via exchange code
        var email = $"google_nopass_{Guid.NewGuid():N}@test.com";
        Guid userId = default;
        await factory.WithDbContextAsync(async db =>
        {
            var role = await db.Roles.FirstAsync(r => r.Name == AppRoles.User);
            var user = new UserEntity
            {
                Email = email,
                UserName = $"gnp_{Guid.NewGuid():N}",
                GoogleId = $"gid_{Guid.NewGuid():N}",
                EmailConfirmed = true,
                PasswordHash = null,
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            user.Roles.Add(role);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
            return true;
        });

        var cache = factory.Services.GetRequiredService<ICacheService>();
        var code = Guid.NewGuid().ToString("N");
        await cache.SetAsync(CacheKeys.GoogleExchange.WithCode(code), userId.ToString(), TimeSpan.FromMinutes(2), CancellationToken.None);

        var client = factory.CreateClient();
        var exchangeResponse = await client.GetAsync($"{GoogleExchangeEndpoint.Route}?code={code}");
        exchangeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokens = await exchangeResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        // Act
        var response = await client.DeleteAsync(DisconnectGoogleEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOOGLE-DISCONNECT-005: disconnect without GoogleId linked is idempotent — returns 200")]
    public async Task Disconnect_UserWithNoGoogleId_Returns200()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DisconnectGoogleEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GOOGLE-DISCONNECT-006: after disconnect, user can still login with email/password")]
    public async Task Disconnect_UserWithPassword_CanStillLoginAfterDisconnect()
    {
        // Arrange
        const string password = "Test@1234!";
        var email = $"disc_login_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, password);
        await factory.WithDbContextAsync(async db =>
        {
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.GoogleId = $"gid_{Guid.NewGuid():N}";
            await db.SaveChangesAsync();
            return true;
        });
        await client.DeleteAsync(DisconnectGoogleEndpoint.Route);
        client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new { email, password });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "GOOGLE-DISCONNECT-007: duplicate GoogleId across users is rejected by DB unique index")]
    public async Task Complete_ConnectMode_DuplicateGoogleId_ViolatesUniqueIndex()
    {
        // Arrange
        var googleId = $"shared_gid_{Guid.NewGuid():N}";
        Guid user2Id = default;
        await factory.WithDbContextAsync(async db =>
        {
            var role = await db.Roles.FirstAsync(r => r.Name == AppRoles.User);
            var user1 = new UserEntity
            {
                Email = $"dup1_{Guid.NewGuid():N}@test.com",
                UserName = $"dup1_{Guid.NewGuid():N}",
                GoogleId = googleId,
                EmailConfirmed = true,
                PasswordHash = null,
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            var user2 = new UserEntity
            {
                Email = $"dup2_{Guid.NewGuid():N}@test.com",
                UserName = $"dup2_{Guid.NewGuid():N}",
                GoogleId = null,
                EmailConfirmed = true,
                PasswordHash = "hashed",
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            user1.Roles.Add(role);
            user2.Roles.Add(role);
            db.Users.AddRange(user1, user2);
            await db.SaveChangesAsync();
            user2Id = user2.Id;
            return true;
        });

        // Act — attempt to assign the same GoogleId to user2 directly
        var conflictSaved = false;
        try
        {
            await factory.WithDbContextAsync(async db =>
            {
                var user = await db.Users.FindAsync([user2Id]);
                user!.GoogleId = googleId;
                await db.SaveChangesAsync();
                return true;
            });
            conflictSaved = true;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException) { }

        // Assert
        conflictSaved.ShouldBeFalse("Unique index should prevent duplicate GoogleId");
    }
}

