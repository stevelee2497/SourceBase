using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Users;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

[EndpointFact(
    Feature = "Users",
    Name = "Delete User",
    Route = "DELETE /api/users/{id}",
    Auth = "Admin only",
    UseCase = "As an admin, I want to delete a user account, so that I can remove deactivated or unwanted accounts from the system.",
    Description = new[]
    {
        "Admin provides the target user `id` (route).",
        "If the user doesn't exist → `404 Not Found`.",
        "The user record is deleted from the database.",
        "Any existing tokens issued to that user are implicitly invalidated because the user no longer exists.",
    })]
public class DeleteUserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-DELETE-001: delete user without token return 401")]
    public async Task DeleteUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteUserEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-DELETE-002: delete user with non admin user return 403")]
    public async Task DeleteUser_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var adminClient = await factory.CreateAuthorizedClient();
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"target_{Guid.NewGuid():N}",
            email = $"target_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await nonAdminClient.DeleteAsync(DeleteUserEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "USERS-DELETE-003: delete user with existing user return 200")]
    public async Task DeleteUser_WithExistingUser_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"managed_{Guid.NewGuid():N}",
            email = $"managed_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteUserEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteUserResponse>();
        body!.Success.ShouldBeTrue();

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        users!.Items.ShouldNotContain(x => x.Id == createBody.Id);
    }

    [Fact(DisplayName = "USERS-DELETE-004: delete user with unknown user return 404")]
    public async Task DeleteUser_WithUnknownUser_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteUserEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "USERS-DELETE-005: delete user with deleted user revokes existing token")]
    public async Task DeleteUser_WithDeletedUser_RevokesExistingToken()
    {
        // Arrange
        var targetEmail = $"deleted_{Guid.NewGuid():N}@test.com";
        var targetClient = await factory.CreateAuthorizedClient(targetEmail, "Test@1234!");

        var targetInfoResponse = await targetClient.GetAsync(GetUserInfoEndpoint.Route);
        var targetInfo = await targetInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        var adminClient = await factory.CreateAuthorizedClient();
        await adminClient.DeleteAsync(DeleteUserEndpoint.Route.WithId(targetInfo!.Id));

        // Act
        var response = await targetClient.GetAsync(GetUserInfoEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
