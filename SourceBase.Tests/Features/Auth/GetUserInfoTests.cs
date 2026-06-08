using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class GetUserInfoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "GET-INFO-001: GetUserInfo_WithValidToken_ReturnsOk")]
    public async Task GetUserInfo_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetUserInfoEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.UserName.Should().Be(WebAppFactory.AdminEmail);
        body.Email.Should().Be(WebAppFactory.AdminEmail);
        body.Roles.Should().Contain("Admin");
    }

    [Fact(DisplayName = "GET-INFO-002: GetUserInfo_WithDistinctUserNameAndEmail_ReturnsMatchingClaims")]
    public async Task GetUserInfo_WithDistinctUserNameAndEmail_ReturnsMatchingClaims()
    {
        // Arrange
        var client = factory.CreateClient();
        var userName = $"claims_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName,
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        // Act
        var response = await client.GetAsync(GetUserInfoEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.Should().NotBeNull();
        body!.UserName.Should().Be(userName);
        body.Email.Should().Be(email);
        body.Roles.Should().Contain("User");
    }

    [Fact(DisplayName = "GET-INFO-003: GetUserInfo_WithoutToken_ReturnsUnauthorized")]
    public async Task GetUserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetUserInfoEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
