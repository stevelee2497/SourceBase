using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class ConfirmEmailTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CONFIRM-EMAIL-001: ConfirmEmail_WithValidCode_ReturnsOk")]
    public async Task ConfirmEmail_WithValidCode_ReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();
        var userName = $"confirm_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName,
            email,
            password = "Test@1234!",
        });
        var code = await factory.GetOtpCode(email);

        // Act
        var response = await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ConfirmEmailResponse>();
        body!.Success.ShouldBeTrue();

        var token = await factory.GetAccessTokenAsync(client, email, "Test@1234!");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var infoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var info = await infoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        info!.EmailConfirmed.ShouldBeTrue();
        info.Roles.ShouldContain("User");
    }


    [Fact(DisplayName = "CONFIRM-EMAIL-002: ConfirmEmail_WithInvalidCode_ReturnsUnauthorized")]
    public async Task ConfirmEmail_WithInvalidCode_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"badcode_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"badcode_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });

        // Act
        var response = await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = "000000",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }


    [Fact(DisplayName = "CONFIRM-EMAIL-003: ConfirmEmail_WithExpiredCode_ReturnsUnauthorized")]
    public async Task ConfirmEmail_WithExpiredCode_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"expired_confirm_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"expired_confirm_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });
        var code = await factory.GetOtpCode(email);
        factory.FakeDateTime.Advance(TimeSpan.FromMinutes(16)); // Assuming OTP expires in 15 minutes

        // Act
        var response = await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }


    [Fact(DisplayName = "CONFIRM-EMAIL-004: ConfirmEmail_WithUnknownEmail_ReturnsUnauthorized")]
    public async Task ConfirmEmail_WithUnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email = "nobody@example.com",
            code = "000000",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }


    [Fact(DisplayName = "CONFIRM-EMAIL-005: ConfirmEmail_WithInvalidPayload_ReturnsBadRequest")]
    public async Task ConfirmEmail_WithInvalidPayload_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email = "not-an-email",
            code = "123",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
