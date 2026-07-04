using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Confirm Email",
    Route = "POST /api/auth/confirmEmail",
    Auth = "Anonymous",
    UseCase = "As a newly registered user, I want to confirm my email with the OTP code I received, so that I can unlock login access to my account.",
    Description = new[]
    {
        "Client sends `email` and `code` (6-character OTP).",
        "The server looks up the user by email — if not found → `401 Unauthorized`.",
        "The OTP code is validated against the stored code and its expiry timestamp.",
        "If invalid or expired → `401 Unauthorized`.",
        "On success, `EmailConfirmed` is set to `true` and the default `User` role is assigned to the account.",
    })]
public class ConfirmEmailTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "CONFIRM-EMAIL-001: valid code returns 200")]
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


    [Fact(DisplayName = "CONFIRM-EMAIL-002: invalid code returns 401")]
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


    [Fact(DisplayName = "CONFIRM-EMAIL-003: expired code returns 401")]
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


    [Fact(DisplayName = "CONFIRM-EMAIL-004: unknown email returns 401")]
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


    [Fact(DisplayName = "CONFIRM-EMAIL-005: invalid payload returns 400")]
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
