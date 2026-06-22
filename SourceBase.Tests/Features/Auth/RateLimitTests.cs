using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class RateLimitTests(RateLimitWebAppFactory factory) : IClassFixture<RateLimitWebAppFactory>
{
    private HttpClient CreateClientWithUniqueIp()
    {
        var client = factory.CreateClient();
        var ip = $"10.{Random.Shared.Next(1, 254)}.{Random.Shared.Next(1, 254)}.{Random.Shared.Next(1, 254)}";
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
        return client;
    }

    [Fact(DisplayName = "RATE-LIMIT-001: Login_ExceedsStrictLimit_Returns429")]
    public async Task Login_ExceedsStrictLimit_Returns429()
    {
        // Arrange
        var client = CreateClientWithUniqueIp();
        var payload = new { email = "bot@example.com", password = "wrong" };

        // Act — exhaust the strict limit bucket
        for (var i = 0; i < RateLimitWebAppFactory.StrictPermitLimit; i++)
            await client.PostAsJsonAsync(LoginEndpoint.Route, payload);

        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "RATE-LIMIT-002: Register_ExceedsStrictLimit_Returns429")]
    public async Task Register_ExceedsStrictLimit_Returns429()
    {
        // Arrange
        var client = CreateClientWithUniqueIp();

        // Act — exhaust the strict limit bucket
        for (var i = 0; i < RateLimitWebAppFactory.StrictPermitLimit; i++)
            await client.PostAsJsonAsync(RegisterEndpoint.Route, new
            {
                userName = $"bot_{Guid.NewGuid():N}",
                email = $"bot_{Guid.NewGuid():N}@example.com",
                password = "Test@1234!",
            });

        var response = await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"bot_{Guid.NewGuid():N}",
            email = $"bot_{Guid.NewGuid():N}@example.com",
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "RATE-LIMIT-003: ForgotPassword_ExceedsStrictLimit_Returns429")]
    public async Task ForgotPassword_ExceedsStrictLimit_Returns429()
    {
        // Arrange
        var client = CreateClientWithUniqueIp();
        var payload = new { email = "bot@example.com" };

        // Act — exhaust the strict limit bucket
        for (var i = 0; i < RateLimitWebAppFactory.StrictPermitLimit; i++)
            await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, payload);

        var response = await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "RATE-LIMIT-004: RateLimitRejection_ReturnsJsonErrorFormat")]
    public async Task RateLimitRejection_ReturnsJsonErrorFormat()
    {
        // Arrange
        var client = CreateClientWithUniqueIp();
        var payload = new { email = "bot@example.com", password = "wrong" };

        // Act — exhaust then trigger
        for (var i = 0; i < RateLimitWebAppFactory.StrictPermitLimit; i++)
            await client.PostAsJsonAsync(LoginEndpoint.Route, payload);

        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, payload);

        // Assert — status and Retry-After header
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").ShouldBeTrue();

        // Assert — JSON body matches GlobalExceptionMiddleware shape
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().ShouldBe("RATE_LIMIT_EXCEEDED");
        body.GetProperty("message").GetString().ShouldBe("Too many requests. Please try again later.");
        body.TryGetProperty("traceId", out _).ShouldBeTrue();
    }
}
