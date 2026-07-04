using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Users;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

[EndpointFact(
    Feature = "Users",
    Name = "Confirm User Email",
    Route = "POST /api/users/{id}/confirmEmail",
    Auth = "Admin only",
    UseCase = "As an admin, I want to manually confirm a user's email, so that I can unblock accounts without requiring the user to go through the email verification flow.",
    Description = new[]
    {
        "Admin provides the target user `id` (route).",
        "If the user doesn't exist → `404 Not Found`.",
        "`EmailConfirmed` is set to `true` on the user record.",
    })]
public class ConfirmUserEmailTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-001: no token returns 401")]
    public async Task ConfirmUserEmail_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(ConfirmUserEmailEndpoint.Route.WithId(Guid.NewGuid()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-002: non-admin user returns 403")]
    public async Task ConfirmUserEmail_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_ce_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await nonAdminClient.PostAsJsonAsync(ConfirmUserEmailEndpoint.Route.WithId(Guid.NewGuid()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-003: non-existent user returns 404")]
    public async Task ConfirmUserEmail_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();

        // Act
        var response = await adminClient.PostAsJsonAsync(ConfirmUserEmailEndpoint.Route.WithId(Guid.NewGuid()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-004: valid user returns 200")]
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
        var response = await adminClient.PostAsJsonAsync(ConfirmUserEmailEndpoint.Route.WithId(created!.Id), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ConfirmUserEmailResponse>();
        body!.Success.ShouldBeTrue();
    }

    [Fact(DisplayName = "USERS-CONFIRM-EMAIL-005: sets email confirmed to true")]
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

        var usersBeforeResponse = await adminClient.GetAsync($"{GetUsersEndpoint.Route}?limit=100");
        var usersBefore = await usersBeforeResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        usersBefore!.Items.Single(x => x.Id == created!.Id).EmailConfirmed.ShouldBeFalse();

        // Act
        await adminClient.PostAsJsonAsync($"users/{created!.Id}/confirm-email", new { });

        // Assert
        var usersAfterResponse = await adminClient.GetAsync($"{GetUsersEndpoint.Route}?limit=100");
        var usersAfter = await usersAfterResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        usersAfter!.Items.Single(x => x.Id == created.Id).EmailConfirmed.ShouldBeTrue();
        var afterEntity = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == created.Id));
        afterEntity.OtpCode.ShouldBeNull();
        afterEntity.OtpCodeExpiresOn.ShouldBeNull();
    }
}
