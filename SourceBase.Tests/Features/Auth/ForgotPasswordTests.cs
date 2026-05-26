using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class ForgotPasswordTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        body!.Success.Should().BeTrue();
        var updatedUser = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Email == email));
        updatedUser.OtpCode.Should().NotBeNullOrEmpty();
        updatedUser.OtpCode.Should().NotBe(originalCode);
        updatedUser.OtpCodeExpiresOn.Should().BeAfter(DateTime.UtcNow);

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());

        latestEmail.Should().NotBeNull();
        latestEmail!.Subject.Should().Be("Reset Password");
        latestEmail.Body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
