using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Roles;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Roles;

public class DeleteRoleTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "ROLES-DELETE-001")]
    [Fact]
    public async Task DeleteRole_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteRoleEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "ROLES-DELETE-002")]
    [Fact]
    public async Task DeleteRole_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var adminClient = await factory.CreateAuthorizedClient();
        var createResponse = await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "Will be deleted",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();

        // Act
        var response = await nonAdminClient.DeleteAsync(DeleteRoleEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    [Trait("TestCaseId", "ROLES-DELETE-003")]
    [Fact]
    public async Task DeleteRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "Will be deleted",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteRoleEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteRoleResponse>();
        body!.Success.Should().BeTrue();

        var rolesResponse = await client.GetAsync(GetRolesEndpoint.Route);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.Should().NotContain(x => x.Id == createBody.Id);
    }
    [Trait("TestCaseId", "ROLES-DELETE-004")]
    [Fact]
    public async Task DeleteRole_WithAdminRole_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var rolesResponse = await client.GetAsync(GetRolesEndpoint.Route);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        var adminRole = roles!.Items.Single(x => x.Name == "Admin");

        // Act
        var response = await client.DeleteAsync(DeleteRoleEndpoint.Route.WithId(adminRole.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Trait("TestCaseId", "ROLES-DELETE-005")]
    [Fact]
    public async Task DeleteRole_WithUnknownId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteRoleEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
