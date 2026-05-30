using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class RefreshTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "REFRESH-001: RefreshToken_WithValidToken_PreservesRoles")]
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
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        refreshBody.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshBody!.AccessToken);

        var getInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        getInfoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body!.Roles.Should().Contain("Admin");
    }

    [Fact(DisplayName = "REFRESH-002: RefreshToken_WithInvalidToken_ReturnsUnauthorized")]
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "REFRESH-003: RefreshToken_AfterLogout_ReturnsUnauthorized")]
    public async Task RefreshToken_AfterLogout_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"refresh_logout_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";

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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "REFRESH-004: RefreshToken_WithMissingToken_ReturnsBadRequest")]
    public async Task RefreshToken_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(RefreshEndpoint.Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
