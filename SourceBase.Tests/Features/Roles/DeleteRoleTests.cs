using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Roles;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Roles;

[EndpointFact(
    Feature = "Roles",
    Name = "Delete Role",
    Route = "DELETE /api/roles/{id}",
    Auth = "Admin only",
    UseCase = "As an admin, I want to delete a role that is no longer needed, so that I can keep the role list clean and accurate.",
    Description = new[]
    {
        "Admin provides the role `id` (route).",
        "If the role doesn't exist → `400 Bad Request`.",
        "The `Admin` role is protected and cannot be deleted → `400 Bad Request`.",
        "The role is removed from the database.",
    })]
public class DeleteRoleTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ROLES-DELETE-001: DeleteRole_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteRole_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteRoleEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ROLES-DELETE-002: DeleteRole_WithNonAdminUser_ReturnsForbidden")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "ROLES-DELETE-003: DeleteRole_WithValidData_ReturnsOk")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteRoleResponse>();
        body!.Success.ShouldBeTrue();

        var rolesResponse = await client.GetAsync(GetRolesEndpoint.Route);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.ShouldNotContain(x => x.Id == createBody.Id);
    }

    [Fact(DisplayName = "ROLES-DELETE-004: DeleteRole_WithAdminRole_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ROLES-DELETE-005: DeleteRole_WithUnknownId_ReturnsBadRequest")]
    public async Task DeleteRole_WithUnknownId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteRoleEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
