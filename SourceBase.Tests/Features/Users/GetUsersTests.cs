using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Users;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class GetUsersTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-GET-001: GetUsers_WithoutToken_ReturnsUnauthorized")]
    public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetUsersEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-GET-002: GetUsers_WithNonAdminUser_ReturnsForbidden")]
    public async Task GetUsers_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await nonAdminClient.GetAsync(GetUsersEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "USERS-GET-003: GetUsers_WithAdminUser_ReturnsCreatedUsers")]
    public async Task GetUsers_WithAdminUser_ReturnsCreatedUsers()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var managedEmail = $"managed_{Guid.NewGuid():N}@test.com";

        var createResponse = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"managed_{Guid.NewGuid():N}",
            email = managedEmail,
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await client.GetAsync(GetUsersEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(x => x.Id == createBody!.Id && x.Email == managedEmail && x.Roles.Contains("User"));
    }

    [Fact(DisplayName = "USERS-GET-004: GetUsers_WithPagingAndOrdering_ReturnsRequestedPage")]
    public async Task GetUsers_WithPagingAndOrdering_ReturnsRequestedPage()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = "alpha_user",
            email = $"alpha_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var expectedCreateResponse = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = "zulu_user",
            email = $"zulu_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var expectedBody = await expectedCreateResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await client.GetAsync($"{GetUsersEndpoint.Route}?orderBy=UserName&order=Desc&page=1&limit=1");

        // Assert
        var users = await response.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        users!.Page.Should().Be(1);
        users.Limit.Should().Be(1);
        users.Items.Should().ContainSingle();
        users.Items.Single().Id.Should().Be(expectedBody!.Id);
    }
}
