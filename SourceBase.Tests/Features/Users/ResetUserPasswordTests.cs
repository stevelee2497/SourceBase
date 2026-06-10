using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Users;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class ResetUserPasswordTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-RESET-PWD-001: ResetUserPassword_WithoutToken_ReturnsUnauthorized")]
    public async Task ResetUserPassword_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ResetUserPasswordEndpoint.Route.WithId(Guid.NewGuid()), new { newPassword = "Test@1234!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-RESET-PWD-002: ResetUserPassword_AsNonAdmin_ReturnsForbidden")]
    public async Task ResetUserPassword_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await nonAdminClient.PostAsJsonAsync(ResetUserPasswordEndpoint.Route.WithId(Guid.NewGuid()), new { newPassword = "Test@1234!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "USERS-RESET-PWD-003: ResetUserPassword_WithNonExistentUser_ReturnsNotFound")]
    public async Task ResetUserPassword_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();

        // Act
        var response = await adminClient.PostAsJsonAsync(ResetUserPasswordEndpoint.Route.WithId(Guid.NewGuid()), new { newPassword = "NewPass@1234!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "USERS-RESET-PWD-004: ResetUserPassword_WithValidData_ReturnsOk")]
    public async Task ResetUserPassword_WithValidData_ReturnsOk()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var userEmail = $"reset_target_{Guid.NewGuid():N}@test.com";
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"reset_user_{Guid.NewGuid():N}",
            email = userEmail,
            password = "Original@1234!",
            roles = new[] { "User" },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await adminClient.PostAsJsonAsync(ResetUserPasswordEndpoint.Route.WithId(created!.Id), new { newPassword = "NewPass@1234!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ResetUserPasswordResponse>();
        body!.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "USERS-RESET-PWD-005: ResetUserPassword_SendsEmailWithNewPassword")]
    public async Task ResetUserPassword_SendsEmailWithNewPassword()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var userEmail = $"reset_email_{Guid.NewGuid():N}@test.com";
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"reset_email_user_{Guid.NewGuid():N}",
            email = userEmail,
            password = "Original@1234!",
            roles = new[] { "User" },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        var newPassword = "Sent@5678!";

        // Act
        await adminClient.PostAsJsonAsync(ResetUserPasswordEndpoint.Route.WithId(created!.Id), new { newPassword });

        // Assert
        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == userEmail)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());
        latestEmail.Should().NotBeNull();
        latestEmail!.Subject.Should().Be("Your password has been reset");
        latestEmail.Body.Should().Contain(newPassword);
    }

    [Fact(DisplayName = "USERS-RESET-PWD-006: ResetUserPassword_WithTooShortPassword_ReturnsBadRequest")]
    public async Task ResetUserPassword_WithTooShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"short_pass_{Guid.NewGuid():N}",
            email = $"short_pass_{Guid.NewGuid():N}@test.com",
            password = "Valid@1234!",
            roles = new[] { "User" },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await adminClient.PostAsJsonAsync(ResetUserPasswordEndpoint.Route.WithId(created!.Id), new { newPassword = "abc" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
