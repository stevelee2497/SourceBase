using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.ShouldNotBeNull();
        body!.Id.ShouldNotBe(Guid.Empty);
        body.UserName.ShouldBe(WebAppFactory.AdminEmail);
        body.Email.ShouldBe(WebAppFactory.AdminEmail);
        body.Roles.ShouldContain("Admin");
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.ShouldNotBeNull();
        body!.UserName.ShouldBe(userName);
        body.Email.ShouldBe(email);
        body.Roles.ShouldContain("User");
    }

    [Fact(DisplayName = "GET-INFO-004: GetUserInfo_ReturnsEmailConfirmedTrue_ForConfirmedUser")]
    public async Task GetUserInfo_ReturnsEmailConfirmedTrue_ForConfirmedUser()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"confirmed_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"confirmed_{Guid.NewGuid():N}",
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        // Act
        var response = await client.GetAsync(GetUserInfoEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.ShouldNotBeNull();
        body!.EmailConfirmed.ShouldBeTrue();
    }

    [Fact(DisplayName = "GET-INFO-003: GetUserInfo_WithoutToken_ReturnsUnauthorized")]
    public async Task GetUserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetUserInfoEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
