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
    Name = "Update Role",
    Route = "PATCH /api/roles/{id}",
    Auth = "Admin only",
    UseCase = "As an admin, I want to partially update a role's name or description, so that I can keep role definitions accurate without resending all fields.",
    Description = new[]
    {
        "Admin sends the role `id` (route) and any subset of: `name`, `description`. All fields are optional — only provided (non-null) fields are updated.",
        "The `Admin` role is protected and cannot be modified → `400 Bad Request`.",
        "If `name` is provided and already used by a different role → `400 Bad Request`.",
        "Updating a role to its current name (no-op rename) is valid and returns 200.",
    })]
public class UpdateRoleTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ROLES-UPDATE-001: without token return 401")]
    public async Task UpdateRole_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateRoleEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "After update",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ROLES-UPDATE-002: non-admin user return 403")]
    public async Task UpdateRole_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var adminClient = await factory.CreateAuthorizedClient();
        var createResponse = await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "Before update",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();

        // Act
        var response = await nonAdminClient.PatchAsJsonAsync(UpdateRoleEndpoint.Route.WithId(createBody!.Id), new
        {
            name = $"Role_{Guid.NewGuid():N}_Updated",
            description = "After update",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "ROLES-UPDATE-003: valid data return 200")]
    public async Task UpdateRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName,
            description = "Before update",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();
        var updatedRoleName = $"{roleName}_Updated";

        // Act
        var response = await client.PatchAsJsonAsync(UpdateRoleEndpoint.Route.WithId(createBody!.Id), new
        {
            name = updatedRoleName,
            description = "After update",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateRoleResponse>();
        body!.Id.ShouldBe(createBody.Id);

        var rolesResponse = await client.GetAsync($"{GetRolesEndpoint.Route}?limit=100");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.ShouldContain(x => x.Id == createBody.Id && x.Name == updatedRoleName && x.Description == "After update");
    }

    [Fact(DisplayName = "ROLES-UPDATE-004: duplicate name (case-insensitive) return 400")]
    public async Task UpdateRole_WithDuplicateNameIgnoringCase_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var firstName = $"first_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = firstName,
            description = "First",
        });
        var secondRoleResponse = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"second_{Guid.NewGuid():N}",
            description = "Second",
        });
        var secondRoleBody = await secondRoleResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateRoleEndpoint.Route.WithId(secondRoleBody!.Id), new
        {
            name = firstName.ToUpperInvariant(),
            description = "Updated",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ROLES-UPDATE-005: admin role cannot be updated return 400")]
    public async Task UpdateRole_WithAdminRole_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var rolesResponse = await client.GetAsync($"{GetRolesEndpoint.Route}?limit=100");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        var adminRole = roles!.Items.Single(x => x.Name == "Admin");

        // Act
        var response = await client.PatchAsJsonAsync(UpdateRoleEndpoint.Route.WithId(adminRole.Id), new
        {
            name = "AdminUpdated",
            description = "Updated",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ROLES-UPDATE-006: same name on same role return 200")]
    public async Task UpdateRole_WithSameNameOnSameRole_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName,
            description = "Before update",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateRoleEndpoint.Route.WithId(createBody!.Id), new
        {
            name = roleName,
            description = "After update",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "ROLES-UPDATE-007: empty id return 400")]
    public async Task UpdateRole_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateRoleEndpoint.Route.WithId(Guid.Empty), new
        {
            name = $"Role_{Guid.NewGuid():N}",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
