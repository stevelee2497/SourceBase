using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Auth;
using SourceBase.Api.Features.Users;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class DeleteUserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "USERS-DELETE-001")]
    [Fact]
    public async Task DeleteUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteUserEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "USERS-DELETE-002")]
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    [Trait("TestCaseId", "USERS-DELETE-003")]
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteUserResponse>();
        body!.Success.Should().BeTrue();

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        users!.Items.Should().NotContain(x => x.Id == createBody.Id);
    }
    [Trait("TestCaseId", "USERS-DELETE-004")]
    [Fact]
    public async Task DeleteUser_WithUnknownUser_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteUserEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Trait("TestCaseId", "USERS-DELETE-005")]
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
