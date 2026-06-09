using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class TokenExpiryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TOKEN-EXPIRY-001: RefreshToken_WhenExpired_ReturnsUnauthorized")]
    public async Task RefreshToken_WhenExpired_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email = WebAppFactory.AdminEmail,
            password = WebAppFactory.AdminPassword,
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Advance fake clock past the 14-day (20160-minute) refresh token lifetime
        factory.FakeDateTime.Advance(TimeSpan.FromMinutes(20161));

        // Act
        var response = await client.PostAsJsonAsync(RefreshEndpoint.Route, new
        {
            token = loginBody!.RefreshToken,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
