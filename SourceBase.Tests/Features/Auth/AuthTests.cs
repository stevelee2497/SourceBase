using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;

namespace SourceBase.Tests.Features.Auth;

[TestFixture]
public class AuthTests
{
    private WebAppFactory _factory = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _factory = new WebAppFactory();
        await _factory.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown() => await _factory.DisposeAsync();

    [Test]
    public async Task Login_WithValidCredentials_ReturnsOkAndAccessToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = WebAppFactory.AdminEmail,
            password = WebAppFactory.AdminPassword,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(WebAppFactory.JsonOptions);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = WebAppFactory.AdminEmail,
            password = "wrong-password",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "any-password",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUserInfo_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync("/api/auth/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>(WebAppFactory.JsonOptions);
        body.Should().NotBeNull();
        body!.Email.Should().Be(WebAppFactory.AdminEmail);
        body.Roles.Should().NotBeEmpty();
    }

    [Test]
    public async Task GetUserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
