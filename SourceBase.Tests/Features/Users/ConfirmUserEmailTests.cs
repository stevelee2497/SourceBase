using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Users;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class ConfirmUserEmailTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-001: ConfirmUserEmail_WithoutToken_ReturnsUnauthorized")]
    public async Task ConfirmUserEmail_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"users/{Guid.NewGuid()}/confirm-email", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-002: ConfirmUserEmail_AsNonAdmin_ReturnsForbidden")]
    public async Task ConfirmUserEmail_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_ce_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await nonAdminClient.PostAsJsonAsync($"users/{Guid.NewGuid()}/confirm-email", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-003: ConfirmUserEmail_WithNonExistentUser_ReturnsNotFound")]
    public async Task ConfirmUserEmail_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();

        // Act
        var response = await adminClient.PostAsJsonAsync($"users/{Guid.NewGuid()}/confirm-email", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-004: ConfirmUserEmail_WithValidUser_ReturnsOk")]
    public async Task ConfirmUserEmail_WithValidUser_ReturnsOk()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var email = $"confirm_email_{Guid.NewGuid():N}@test.com";
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"confirm_user_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await adminClient.PostAsJsonAsync($"users/{created!.Id}/confirm-email", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ConfirmUserEmailResponse>();
        body!.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-005: ConfirmUserEmail_SetsEmailConfirmedTrue")]
    public async Task ConfirmUserEmail_SetsEmailConfirmedTrue()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var email = $"set_confirmed_{Guid.NewGuid():N}@test.com";
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"set_confirmed_user_{Guid.NewGuid():N}",
            email,
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        var before = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == created!.Id));
        before.EmailConfirmed.Should().BeFalse();

        // Act
        await adminClient.PostAsJsonAsync($"users/{created!.Id}/confirm-email", new { });

        // Assert
        var after = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == created.Id));
        after.EmailConfirmed.Should().BeTrue();
        after.OtpCode.Should().BeNull();
        after.OtpCodeExpiresOn.Should().BeNull();
    }
}
