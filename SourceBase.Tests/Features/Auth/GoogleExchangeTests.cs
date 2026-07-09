using System.Net;
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
    Name = "Google Exchange",
    Route = "GET /api/auth/google/exchange",
    Auth = "Anonymous",
    UseCase = "As a user returning from Google OAuth, I want to exchange my short-lived code for bearer tokens.",
    Description = new[]
    {
        "GET /api/auth/google/exchange?code={uuid}",
        "The code must exist in cache (set by /complete). Returns 200 with bearer tokens.",
        "Response includes accessToken, refreshToken, tokenType, and expiresIn.",
        "The code is one-time-use: a second call with the same code returns 401.",
        "An unknown code returns 401.",
        "Missing code query param returns 400.",
    })]
public class GoogleExchangeTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    private async Task<(string code, HttpClient client)> SeedUserAndCodeAsync()
    {
        Guid userId = default;
        await factory.WithDbContextAsync(async db =>
        {
            var role = await db.Roles.FirstAsync(r => r.Name == AppRoles.User);
            var user = new UserEntity
            {
                Email = $"exchange_{Guid.NewGuid():N}@test.com",
                UserName = $"exch_{Guid.NewGuid():N}",
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
        return (code, factory.CreateClient());
    }

    [RequiresRedisFact(DisplayName = "GOOGLE-EXCHANGE-001: valid code returns 200 with access token")]
    public async Task Exchange_ValidCode_Returns200WithAccessToken()
    {
        // Arrange
        var (code, client) = await SeedUserAndCodeAsync();

        // Act
        var response = await client.GetAsync($"{GoogleExchangeEndpoint.Route}?code={code}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.ShouldNotBeNull();
        body!.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [RequiresRedisFact(DisplayName = "GOOGLE-EXCHANGE-002: exchange response includes refresh token and token type")]
    public async Task Exchange_ValidCode_ResponseIncludesFullTokenShape()
    {
        // Arrange
        var (code, client) = await SeedUserAndCodeAsync();

        // Act
        var response = await client.GetAsync($"{GoogleExchangeEndpoint.Route}?code={code}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.ShouldNotBeNull();
        body!.AccessToken.ShouldNotBeNullOrEmpty();
        body.RefreshToken.ShouldNotBeNullOrEmpty();
        body.TokenType.ShouldNotBeNullOrEmpty();
        body.ExpiresIn.ShouldBeGreaterThan(0);
    }

    [RequiresRedisFact(DisplayName = "GOOGLE-EXCHANGE-003: code is one-time-use — second call returns 401")]
    public async Task Exchange_SameCodeTwice_SecondCallReturns401()
    {
        // Arrange
        var (code, client) = await SeedUserAndCodeAsync();
        await client.GetAsync($"{GoogleExchangeEndpoint.Route}?code={code}");

        // Act — second call
        var response = await client.GetAsync($"{GoogleExchangeEndpoint.Route}?code={code}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GOOGLE-EXCHANGE-004: unknown code returns 401")]
    public async Task Exchange_UnknownCode_Returns401()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{GoogleExchangeEndpoint.Route}?code={Guid.NewGuid():N}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GOOGLE-EXCHANGE-005: missing code query param returns 400")]
    public async Task Exchange_MissingCodeParam_Returns400()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GoogleExchangeEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
