using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class ResendConfirmationEmailTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "RESEND-CONF-001: ResendConfirmationEmail_WithValidEmail_ReturnsOk")]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ResendConfirmationEmailResponse>();
        body!.Success.Should().BeTrue();

        var updatedUser = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Email == email));
        updatedUser.OtpCode.Should().NotBe(originalCode);
        updatedUser.OtpCodeExpiresOn.Should().BeAfter(factory.FakeDateTime.UtcNow);

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());

        latestEmail.Should().NotBeNull();
        latestEmail!.Subject.Should().Be("Confirm your email");
        latestEmail.Body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "RESEND-CONF-002: ResendConfirmationEmail_WithConfirmedEmail_ReturnsBadRequest")]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "RESEND-CONF-003: ResendConfirmationEmail_WithUnknownEmail_ReturnsNotFound")]
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
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "RESEND-CONF-004: ResendConfirmationEmail_WithInvalidEmail_ReturnsBadRequest")]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
