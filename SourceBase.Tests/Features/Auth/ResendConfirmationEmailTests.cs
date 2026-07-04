using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Resend Confirmation Email",
    Route = "POST /api/auth/resendConfirmationEmail",
    Auth = "Anonymous",
    UseCase = "As a user whose confirmation email expired or was lost, I want to request a new confirmation code, so that I can complete my email verification.",
    Description = new[]
    {
        "Client sends their registered `email`.",
        "If the user is not found → `404 Not Found`.",
        "If the email is already confirmed → `400 Bad Request`.",
        "A new OTP code is generated and stored with a fresh expiry timestamp.",
        "A new confirmation email is sent to the user.",
    })]
public class ResendConfirmationEmailTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "RESEND-CONF-001: valid email returns 200")]
    public async Task ResendConfirmationEmail_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"resend_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"resend_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });
        var originalCode = await factory.GetOtpCode(email);

        // Act
        var response = await client.PostAsJsonAsync(ResendConfirmationEmailEndpoint.Route, new
        {
            email,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ResendConfirmationEmailResponse>();
        body!.Success.ShouldBeTrue();

        var updatedUser = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Email == email));
        updatedUser.OtpCode.ShouldNotBe(originalCode);
        updatedUser.OtpCodeExpiresOn!.Value.ShouldBeGreaterThan(factory.FakeDateTime.UtcNow);

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());

        latestEmail.ShouldNotBeNull();
        latestEmail!.Subject.ShouldBe("Confirm your email");
        latestEmail.Body.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "RESEND-CONF-002: confirmed email returns 400")]
    public async Task ResendConfirmationEmail_WithConfirmedEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"resend_confirmed_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"resend_confirmed_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });

        // Act
        var response = await client.PostAsJsonAsync(ResendConfirmationEmailEndpoint.Route, new
        {
            email,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "RESEND-CONF-003: unknown email returns 404")]
    public async Task ResendConfirmationEmail_WithUnknownEmail_ReturnsNotFound()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ResendConfirmationEmailEndpoint.Route, new
        {
            email = "nobody@example.com",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "RESEND-CONF-004: invalid email returns 400")]
    public async Task ResendConfirmationEmail_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ResendConfirmationEmailEndpoint.Route, new
        {
            email = "not-an-email",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
