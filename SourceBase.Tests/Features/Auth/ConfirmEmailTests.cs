using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class ConfirmEmailTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "CONFIRM-EMAIL-001")]
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ConfirmEmailResponse>();
        body!.Success.Should().BeTrue();

        var dbUser = await factory.WithDbContextAsync(db => db.Users.Include(x => x.Roles).SingleAsync(x => x.Email == email));
        dbUser.EmailConfirmed.Should().BeTrue();
        dbUser.Roles.Should().Contain(x => x.Name == "User");
    }

    [Trait("TestCaseId", "CONFIRM-EMAIL-002")]
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Trait("TestCaseId", "CONFIRM-EMAIL-003")]
    [Fact]
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
        await factory.WithDbContextAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Email == email);
            user.OtpCodeExpiresOn = DateTime.UtcNow.AddMinutes(-1);
            return await db.SaveChangesAsync();
        });

        // Act
        var response = await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Trait("TestCaseId", "CONFIRM-EMAIL-004")]
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Trait("TestCaseId", "CONFIRM-EMAIL-005")]
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
