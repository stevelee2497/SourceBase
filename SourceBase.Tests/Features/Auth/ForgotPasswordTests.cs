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
    Name = "Forgot Password",
    Route = "POST /api/auth/forgotPassword",
    Auth = "Anonymous",
    UseCase = "As a user who forgot their password, I want to request a password reset code by email, so that I can set a new password.",
    Description = new[]
    {
        "Client sends their registered `email`.",
        "If the user is not found → `404 Not Found`.",
        "A new 6-digit OTP code is generated and stored on the user record with an expiry timestamp.",
        "An email containing the reset code is sent to the user.",
        "Returns `{ success: true }`.",
    })]
public class ForgotPasswordTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "FORGOT-PWD-001: ForgotPassword_WithValidEmail_ReturnsOk")]
    public async Task ForgotPassword_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"forgot_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"forgot_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });
        var originalCode = await factory.GetOtpCode(email);

        // Act
        var response = await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, new
        {
            email,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        body!.Success.ShouldBeTrue();
        var updatedUser = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Email == email));
        updatedUser.OtpCode.ShouldNotBeNullOrEmpty();
        updatedUser.OtpCode.ShouldNotBe(originalCode);
        updatedUser.OtpCodeExpiresOn!.Value.ShouldBeGreaterThan(factory.FakeDateTime.UtcNow);

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());

        latestEmail.ShouldNotBeNull();
        latestEmail!.Subject.ShouldBe("Reset Password");
        latestEmail.Body.ShouldNotBeNullOrWhiteSpace();
    }


    [Fact(DisplayName = "FORGOT-PWD-002: ForgotPassword_WithUnknownEmail_ReturnsNotFound")]
    public async Task ForgotPassword_WithUnknownEmail_ReturnsNotFound()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, new
        {
            email = "nobody@example.com",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }


    [Fact(DisplayName = "FORGOT-PWD-003: ForgotPassword_WithInvalidEmail_ReturnsBadRequest")]
    public async Task ForgotPassword_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, new
        {
            email = "not-an-email",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
