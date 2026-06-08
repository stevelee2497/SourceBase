using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class ResetPasswordTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "RESET-PWD-001: ResetPassword_WithValidToken_ReturnsOk")]
    public async Task ResetPassword_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"reset_{Guid.NewGuid():N}@test.com";
        const string oldPassword = "Test@1234!";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"reset_{Guid.NewGuid():N}",
            email,
            password = oldPassword,
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });
        await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, new
        {
            email,
        });
        var code = await factory.GetOtpCode(email);
        const string newPassword = "NewTest@5678!";

        // Act
        var response = await client.PostAsJsonAsync(ResetPasswordEndpoint.Route, new
        {
            email,
            code,
            newPassword,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ResetPasswordResponse>();
        body!.Success.Should().BeTrue();

        var dbUser = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Email == email));
        dbUser.OtpCode.Should().BeNull();
        dbUser.OtpCodeExpiresOn.Should().BeNull();

        var oldPasswordResponse = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email,
            password = oldPassword,
        });
        oldPasswordResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "RESET-PWD-002: ResetPassword_AfterReset_CanLoginWithNewPassword")]
    public async Task ResetPassword_AfterReset_CanLoginWithNewPassword()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"reset_login_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"reset_login_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });
        await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, new
        {
            email,
        });
        var code = await factory.GetOtpCode(email);
        const string newPassword = "NewTest@5678!";

        await client.PostAsJsonAsync(ResetPasswordEndpoint.Route, new
        {
            email,
            code,
            newPassword,
        });

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email,
            password = newPassword,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "RESET-PWD-003: ResetPassword_WithInvalidCode_ReturnsBadRequest")]
    public async Task ResetPassword_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"reset_bad_code_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"reset_bad_code_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });

        // Act
        var response = await client.PostAsJsonAsync(ResetPasswordEndpoint.Route, new
        {
            email,
            code = "000000",
            newPassword = "NewTest@5678!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "RESET-PWD-004: ResetPassword_WithExpiredCode_ReturnsBadRequest")]
    public async Task ResetPassword_WithExpiredCode_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"reset_expired_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(RegisterEndpoint.Route, new
        {
            userName = $"reset_expired_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
        });
        await client.PostAsJsonAsync(ConfirmEmailEndpoint.Route, new
        {
            email,
            code = await factory.GetOtpCode(email),
        });
        await client.PostAsJsonAsync(ForgotPasswordEndpoint.Route, new
        {
            email,
        });
        var code = await factory.GetOtpCode(email);
        await factory.WithDbContextAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Email == email);
            user.OtpCodeExpiresOn = DateTime.UtcNow.AddMinutes(-1);
            return await db.SaveChangesAsync();
        });

        // Act
        var response = await client.PostAsJsonAsync(ResetPasswordEndpoint.Route, new
        {
            email,
            code,
            newPassword = "NewTest@5678!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "RESET-PWD-005: ResetPassword_WithUnknownEmail_ReturnsNotFound")]
    public async Task ResetPassword_WithUnknownEmail_ReturnsNotFound()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ResetPasswordEndpoint.Route, new
        {
            email = "nobody@example.com",
            code = "000000",
            newPassword = "NewTest@5678!",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
