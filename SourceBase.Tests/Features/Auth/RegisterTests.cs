using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class RegisterTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "REGISTER-001")]
    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();
        var userName = $"register_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@test.com";

        // Act
        var response = await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName,
            email,
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.Id.Should().NotBeEmpty();

        var user = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == body.Id));
        user.EmailConfirmed.Should().BeFalse();
        user.OtpCode.Should().NotBeNullOrEmpty();
        user.OtpCodeExpiresOn.Should().NotBeNull();
        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());

        latestEmail.Should().NotBeNull();
        latestEmail!.Subject.Should().Be("Confirm your email");
        latestEmail.Body.Should().NotBeNullOrWhiteSpace();
    }

    [Trait("TestCaseId", "REGISTER-002")]
    [Fact]
    public async Task Register_WithWhitespaceAroundEmailAndUserName_TrimsInputBeforeValidation()
    {
        // Arrange
        var client = factory.CreateClient();
        var trimmedUserName = $"trimmed_{Guid.NewGuid():N}";
        var trimmedEmail = $"{Guid.NewGuid():N}@test.com";

        // Act
        var response = await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"  {trimmedUserName}  ",
            email = $"  {trimmedEmail}  ",
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Email == trimmedEmail));
        user.UserName.Should().Be(trimmedUserName);
    }

    [Trait("TestCaseId", "REGISTER-003")]
    [Fact]
    public async Task Register_WithPasswordContainingOuterSpaces_TrimsPasswordBeforePersisting()
    {
        // Arrange
        var client = factory.CreateClient();
        var userName = $"space_pwd_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@test.com";
        var password = "  Test@1234!  ";

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

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email,
            password = password.Trim(),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Trait("TestCaseId", "REGISTER-004")]
    [Fact]
    public async Task Register_WithDuplicateEmailIgnoringCase_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"dup_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });

        // Act
        var response = await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"dup_{Guid.NewGuid():N}",
            email = email.ToUpperInvariant(),
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Trait("TestCaseId", "REGISTER-005")]
    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"invalid_{Guid.NewGuid():N}",
            email = "not-an-email",
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Trait("TestCaseId", "REGISTER-006")]
    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"shortpw_{Guid.NewGuid():N}",
            email = $"{Guid.NewGuid():N}@test.com",
            password = "123",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
