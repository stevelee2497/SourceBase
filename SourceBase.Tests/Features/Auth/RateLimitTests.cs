using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Login Rate Limiting",
    Route = "POST /api/auth/login",
    Auth = "Anonymous",
    UseCase = "As the system, I want to enforce a strict per-IP rate limit on sensitive anonymous auth endpoints, so that bot abuse, credential stuffing, and email-spam attacks are mitigated.",
    Description = new[]
    {
        "The `strict` policy applies a tighter per-IP limit (default 10 requests / 60 s) to sensitive anonymous auth endpoints, including `auth/login`, `auth/register`, and `auth/forgotPassword`.",
        "Exceeding the strict limit on `auth/login` returns `429 Too Many Requests` with a `Retry-After` header.",
        "Exceeding the strict limit on `auth/register` returns `429 Too Many Requests`.",
        "Exceeding the strict limit on `auth/forgotPassword` returns `429 Too Many Requests`.",
        "The 429 response body follows the standard `GlobalExceptionMiddleware` JSON error format (`traceId`, `code`, `message`, `errors`).",
    })]
public class RateLimitTests(RateLimitWebAppFactory factory) : IClassFixture<RateLimitWebAppFactory>
{
    [Fact(DisplayName = "RATE-LIMIT-001: login exceeds strict limit returns 429")]
    public async Task Login_ExceedsStrictLimit_Returns429()
    {
        // Arrange — each CreateClient() call gets a unique IP so buckets don't bleed between tests
        var client = factory.CreateClient();
        var payload = new { email = "bot@example.com", password = "wrong" };

        // Act — exhaust the strict limit bucket
        for (var i = 0; i < RateLimitWebAppFactory.StrictPermitLimit; i++)
            await client.PostAsJsonAsync(LoginEndpoint.Route, payload);

        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "RATE-LIMIT-002: register exceeds strict limit returns 429")]
    public async Task Register_ExceedsStrictLimit_Returns429()
    {
        // Arrange
        var client = factory.CreateClient();

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

    [Fact(DisplayName = "RATE-LIMIT-003: forgot password exceeds strict limit returns 429")]
    public async Task ForgotPassword_ExceedsStrictLimit_Returns429()
    {
        // Arrange
        var client = factory.CreateClient();
        var payload = new { email = "bot@example.com" };

        // Act — exhaust the strict limit bucket
        for (var i = 0; i < RateLimitWebAppFactory.StrictPermitLimit; i++)
            await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, payload);

        var response = await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "RATE-LIMIT-004: rate limit rejection returns json error format")]
    public async Task RateLimitRejection_ReturnsJsonErrorFormat()
    {
        // Arrange
        var client = factory.CreateClient();
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
