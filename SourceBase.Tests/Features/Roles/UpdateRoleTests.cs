using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Roles;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Roles;

public class UpdateRoleTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ROLES-UPDATE-001: UpdateRole_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateRole_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateRoleEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "After update",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ROLES-UPDATE-002: UpdateRole_WithNonAdminUser_ReturnsForbidden")]
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
        var response = await nonAdminClient.PutAsJsonAsync(UpdateRoleEndpoint.Route.WithId(createBody!.Id), new
        {
            name = $"Role_{Guid.NewGuid():N}_Updated",
            description = "After update",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "ROLES-UPDATE-003: UpdateRole_WithValidData_ReturnsOk")]
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
        var response = await client.PutAsJsonAsync(UpdateRoleEndpoint.Route.WithId(createBody!.Id), new
        {
            name = updatedRoleName,
            description = "After update",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateRoleResponse>();
        body!.Id.Should().Be(createBody.Id);

        var rolesResponse = await client.GetAsync($"{GetRolesEndpoint.Route}?limit=100");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.Should().Contain(x => x.Id == createBody.Id && x.Name == updatedRoleName && x.Description == "After update");
    }

    [Fact(DisplayName = "ROLES-UPDATE-004: UpdateRole_WithDuplicateNameIgnoringCase_ReturnsBadRequest")]
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
        var response = await client.PutAsJsonAsync(UpdateRoleEndpoint.Route.WithId(secondRoleBody!.Id), new
        {
            name = firstName.ToUpperInvariant(),
            description = "Updated",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ROLES-UPDATE-005: UpdateRole_WithAdminRole_ReturnsBadRequest")]
    public async Task UpdateRole_WithAdminRole_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var rolesResponse = await client.GetAsync($"{GetRolesEndpoint.Route}?limit=100");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        var adminRole = roles!.Items.Single(x => x.Name == "Admin");

        // Act
        var response = await client.PutAsJsonAsync(UpdateRoleEndpoint.Route.WithId(adminRole.Id), new
        {
            name = "AdminUpdated",
            description = "Updated",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ROLES-UPDATE-006: UpdateRole_WithSameNameOnSameRole_ReturnsOk")]
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
        var response = await client.PutAsJsonAsync(UpdateRoleEndpoint.Route.WithId(createBody!.Id), new
        {
            name = roleName,
            description = "After update",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
