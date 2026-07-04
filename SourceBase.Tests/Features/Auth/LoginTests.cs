using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Login",
    Route = "POST /api/auth/login",
    Auth = "Anonymous",
    UseCase = "As a registered user, I want to log in with my email and password, so that I can receive an access token and refresh token to make authenticated requests.",
    Description = new[]
    {
        "Client sends `email` and `password`.",
        "The server looks up the user by email.",
        "If the user doesn't exist, the email is not confirmed, or the password is wrong → `401 Unauthorized`.",
        "On success, the JWT middleware issues an access token (JWT) and a refresh token in the response.",
        "The response includes `expiresIn` (seconds), which reflects the configured `AccessTokenExpirationMinutes` in `AppSettings`.",
    })]
public class LoginTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CODE-001: valid credentials return 200 and access token")]
    public async Task Login_WithValidCredentials_ReturnsOkAndAccessToken()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email = WebAppFactory.AdminEmail,
            password = WebAppFactory.AdminPassword,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.ShouldNotBeNull();
        body!.AccessToken.ShouldNotBeNullOrEmpty();
        body.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "CODE-002: wrong password return 401")]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email = WebAppFactory.AdminEmail,
            password = "wrong-password",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CODE-003: unknown email return 401")]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email = "nobody@example.com",
            password = "any-password",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CODE-004: unconfirmed email return 401")]
    public async Task Login_WithUnconfirmedEmail_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"unconfirmed_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"unconfirmed_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email,
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "CODE-005: confirmed email return 200 and access token")]
    public async Task Login_AfterEmailConfirmed_ReturnsOkAndAccessToken()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"login_ok_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"login_ok_{Guid.NewGuid():N}",
            email,
            password,
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email,
            password,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.ShouldNotBeNull();
        body!.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "CODE-006: missing password return 400")]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new { email = WebAppFactory.AdminEmail });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "CODE-007: expiresIn reflects configured token expiration")]
    public async Task Login_ExpiresIn_ReflectsConfiguredAccessTokenLifetime()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email = WebAppFactory.AdminEmail,
            password = WebAppFactory.AdminPassword,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.ExpiresIn.ShouldBe(60 * 60); // 60 minutes configured → 3600 seconds
    }
}
