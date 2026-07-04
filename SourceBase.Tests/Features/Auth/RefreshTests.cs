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
    Name = "Refresh Token",
    Route = "POST /api/auth/refresh",
    Auth = "Anonymous",
    UseCase = "As an authenticated user whose access token has expired, I want to exchange my refresh token for a new access token, so that I can continue using the app without re-entering my credentials.",
    Description = new[]
    {
        "Client sends the `token` (refresh token string).",
        "The server parses the refresh token and extracts `userId` and `securityStamp`.",
        "The user is loaded from the database; if not found → `401 Unauthorized`.",
        "The stored security stamp is compared with the one in the token — mismatch → `401 Unauthorized` (covers logged-out or password-changed scenarios).",
        "On success, a new access token (and refresh token) are issued via the JWT middleware.",
    })]
public class RefreshTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "REFRESH-001: valid token preserves roles")]
    public async Task RefreshToken_WithValidToken_PreservesRoles()
    {
        // Arrange
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email = WebAppFactory.AdminEmail,
            password = WebAppFactory.AdminPassword,
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act
        var refreshResponse = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            token = loginBody!.RefreshToken,
        });

        // Assert
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        refreshBody.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshBody!.AccessToken);

        var getInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        getInfoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body!.Roles.ShouldContain("Admin");
    }

    [Fact(DisplayName = "REFRESH-002: invalid token returns 401")]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            token = "invalid-token",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "REFRESH-003: after logout returns 401")]
    public async Task RefreshToken_AfterLogout_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"refresh_logout_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!_Aokfn1";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"refresh_logout_{Guid.NewGuid():N}",
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

    [Fact(DisplayName = "REFRESH-005: after password reset returns 401")]
    public async Task RefreshToken_AfterPasswordReset_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"refresh_pwdreset_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!_Aokfn1";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"refresh_pwdreset_{Guid.NewGuid():N}",
            email,
            password,
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });
        var loginResponse = await client.PostAsJsonAsync(LoginEndpoint.Route, new { email, password });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var oldRefreshToken = loginBody!.RefreshToken;

        await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, new { email });
        await client.PostAsJsonAsync(ResetPasswordEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
            newPassword = "NewTest@5678!_Aokfn1",
        });

        // Act
        var response = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            token = oldRefreshToken,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "REFRESH-006: new token from refresh response can refresh again")]
    public async Task RefreshToken_NewTokenFromRefreshResponse_CanRefreshAgain()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"refresh_chain_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!_Aokfn1";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"refresh_chain_{Guid.NewGuid():N}",
            email,
            password,
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });
        var loginResponse = await client.PostAsJsonAsync(LoginEndpoint.Route, new { email, password });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var firstRefreshResponse = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            token = loginBody!.RefreshToken,
        });
        var firstRefreshBody = await firstRefreshResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act
        var secondRefreshResponse = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            token = firstRefreshBody!.RefreshToken,
        });

        // Assert
        secondRefreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondRefreshBody = await secondRefreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        secondRefreshBody.ShouldNotBeNull();
        secondRefreshBody!.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "REFRESH-004: missing token returns 400")]
    public async Task RefreshToken_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(RefreshEndpoint.Route, new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
