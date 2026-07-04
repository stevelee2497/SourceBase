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
    Name = "Register",
    Route = "POST /api/auth/register",
    Auth = "Anonymous",
    UseCase = "As a new user, I want to register an account with my username, email, and password, so that I can access the application after confirming my email.",
    Description = new[]
    {
        "Client sends `userName`, `email`, and `password`.",
        "Username and email are trimmed of surrounding whitespace before processing.",
        "If the username or email is already taken → `400 Bad Request`.",
        "A new user is created with a hashed password and a 6-digit OTP confirmation code.",
        "A confirmation email containing the OTP code is sent to the user's email address.",
        "Returns the new user's `Id`.",
    })]
public class RegisterTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "REGISTER-001: Register_WithValidData_ReturnsOk")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);

        var user = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == body.Id));
        user.EmailConfirmed.ShouldBeFalse();
        user.OtpCode.ShouldNotBeNullOrEmpty();
        user.OtpCodeExpiresOn.ShouldNotBeNull();
        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());

        latestEmail.ShouldNotBeNull();
        latestEmail!.Subject.ShouldBe("Confirm your email");
        latestEmail.Body.ShouldNotBeNullOrWhiteSpace();
    }


    [Fact(DisplayName = "REGISTER-002: Register_WithWhitespaceAroundEmailAndUserName_TrimsInputBeforeValidation")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Email == trimmedEmail));
        user.UserName.ShouldBe(trimmedUserName);
    }


    [Fact(DisplayName = "REGISTER-003: Register_WithPasswordContainingOuterSpaces_TrimsPasswordBeforePersisting")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }


    [Fact(DisplayName = "REGISTER-004: Register_WithDuplicateEmailIgnoringCase_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }


    [Fact(DisplayName = "REGISTER-005: Register_WithInvalidEmail_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }


    [Fact(DisplayName = "REGISTER-006: Register_WithShortPassword_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
