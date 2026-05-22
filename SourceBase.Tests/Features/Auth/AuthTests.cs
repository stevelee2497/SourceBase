using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class AuthTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"newuser_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234!" });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"shortpw_{Guid.NewGuid():N}@test.com",
            password = "123",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Confirm email ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmail_WithValidCode_ReturnsNoContent()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"confirm_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234!" });
        var code = await factory.GetLatestEmailCodeAsync(email);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/confirmEmail", new { email, code });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidCode_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"badcode_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234!" });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/confirmEmail", new
        {
            email,
            code = "000000", // valid length but won't match the real OTP
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmEmail_WithUnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/confirmEmail", new
        {
            email = "nobody@example.com",
            code = "000000",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndAccessToken()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = WebAppFactory.AdminEmail,
            password = WebAppFactory.AdminPassword,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = WebAppFactory.AdminEmail,
            password = "wrong-password",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "any-password",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"unconfirmed_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234!" });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Test@1234!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AfterEmailConfirmed_ReturnsOkAndAccessToken()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"login_ok_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        var code = await factory.GetLatestEmailCodeAsync(email);
        await client.PostAsJsonAsync("/api/auth/confirmEmail", new { email, code });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    // ── Get user info ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserInfo_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync("/api/auth/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be(WebAppFactory.AdminEmail);
        body.Roles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserInfo_ReturnsIdEmailAndRoles()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync("/api/auth/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Email.Should().Be(WebAppFactory.AdminEmail);
        body.Roles.Should().Contain("Admin");
    }

    // ── Forgot password ───────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsNoContent()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgotPassword", new
        {
            email = WebAppFactory.AdminEmail,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_ReturnsNotFound()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgotPassword", new
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
        var response = await client.PostAsJsonAsync("/api/auth/forgotPassword", new
        {
            email = "not-an-email",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Reset password ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsNoContent()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"reset_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        var code = await factory.GetLatestEmailCodeAsync(email);
        await client.PostAsJsonAsync("/api/auth/confirmEmail", new { email, code });
        await client.PostAsJsonAsync("/api/auth/forgotPassword", new { email });
        var newCode = await factory.GetLatestEmailCodeAsync(email);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/resetPassword", new
        {
            email,
            code = newCode,
            newPassword = "NewTest@5678!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_AfterReset_CanLoginWithNewPassword()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"reset_login_{Guid.NewGuid():N}@test.com";
        const string oldPassword = "Test@1234!";
        const string newPassword = "NewTest@5678!";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = oldPassword });
        var code = await factory.GetLatestEmailCodeAsync(email);
        await client.PostAsJsonAsync("/api/auth/confirmEmail", new { email, code });
        await client.PostAsJsonAsync("/api/auth/forgotPassword", new { email });
        var newCode = await factory.GetLatestEmailCodeAsync(email);
        await client.PostAsJsonAsync("/api/auth/resetPassword", new { email, code = newCode, newPassword });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"reset_bad_code_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234!" });
        var code = await factory.GetLatestEmailCodeAsync(email);
        await client.PostAsJsonAsync("/api/auth/confirmEmail", new { email, code });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/resetPassword", new
        {
            email,
            code = "000000", // valid length but won't match the real OTP
            newPassword = "NewTest@5678!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithUnknownEmail_ReturnsNotFound()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/resetPassword", new
        {
            email = "nobody@example.com",
            code = "000000",
            newPassword = "NewTest@5678!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Update user info ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/info", new
        {
            firstName = "John",
            lastName = "Doe",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserInfo_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/info", new
        {
            firstName = "Admin",
            lastName = "User",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateUserInfo_ChangesReflectedInGetUserInfo()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var firstName = $"First_{Guid.NewGuid():N}";
        var lastName = $"Last_{Guid.NewGuid():N}";

        // Act
        await client.PutAsJsonAsync("/api/auth/info", new { firstName, lastName });

        // Assert
        var response = await client.GetAsync("/api/auth/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.Should().NotBeNull();
        body!.FirstName.Should().Be(firstName);
        body.LastName.Should().Be(lastName);
    }

    [Fact]
    public async Task UpdateUserInfo_WithRoles_InvalidatesCurrentTokenAndUpdatesCurrentUserRoles()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"updaterole_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        var code = await factory.GetLatestEmailCodeAsync(email);
        await client.PostAsJsonAsync("/api/auth/confirmEmail", new { email, code });
        var token = await Utilities.GetAccessTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var updateResponse = await client.PutAsJsonAsync("/api/auth/info", new
        {
            roles = new[] { "Admin", "User" }
        });

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var responseWithOldToken = await client.GetAsync("/api/auth/info");
        responseWithOldToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newToken = await Utilities.GetAccessTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        var response = await client.GetAsync("/api/auth/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.Should().NotBeNull();
        body!.Roles.Should().Contain("Admin");
        body.Roles.Should().Contain("User");
    }


    // ── Logout ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getInfoResponse = await client.GetAsync("/api/auth/info");
        getInfoResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
