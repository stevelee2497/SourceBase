using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class LogoutTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsync(LogoutEndpoint.Route, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        getInfoResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync(LogoutEndpoint.Route, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
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
        var refreshToken = loginBody!.RefreshToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);
        await client.PostAsync(LogoutEndpoint.Route, null);

        // Act
        var response = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            refreshToken,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
