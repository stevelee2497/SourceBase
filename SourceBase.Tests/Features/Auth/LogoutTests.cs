using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Logout",
    Route = "POST /api/auth/logout",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to log out, so that my access and refresh tokens are immediately invalidated on the server.",
    Description = new[]
    {
        "Client calls the endpoint with a valid access token.",
        "The server loads the current user and rotates their security stamp (a new `Guid`).",
        "Any previously issued tokens that embed the old security stamp are rejected on next use.",
        "Returns `{ success: true }`.",
    })]
public class LogoutTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "LOGOUT-001: Logout_WithValidToken_ReturnsOk")]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsync(LogoutEndpoint.Route, null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var getInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        getInfoResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "LOGOUT-002: Logout_WithoutToken_ReturnsUnauthorized")]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync(LogoutEndpoint.Route, null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "LOGOUT-003: Logout_WithValidToken_InvalidatesRefreshToken")]
    public async Task Logout_WithValidToken_InvalidatesRefreshToken()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"logout_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"logout_{Guid.NewGuid():N}",
            email,
            password,
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });
        var loginResponse = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email,
            password,
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginBody!.RefreshToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);
        await client.PostAsync(LogoutEndpoint.Route, null);

        // Act
        var response = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            token,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
