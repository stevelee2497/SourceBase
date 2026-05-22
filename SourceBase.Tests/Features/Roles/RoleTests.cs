using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Roles;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Roles;

public class RoleTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task CreateRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";
        var description = "Created in integration test";

        // Act
        var response = await client.PostAsJsonAsync("/api/roles", new { name = roleName, description });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateRoleResponse>();
        body!.Id.Should().NotBeEmpty();
        var rolesResponse = await client.GetAsync("/api/roles");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles.Should().NotBeNull();
        roles!.Items.Should().ContainSingle(x => x.Name == roleName && x.Description == description);
    }

    [Fact]
    public async Task UpdateRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/roles", new { name = roleName, description = "Before update" });
        var role = await GetRoleByNameAsync(client, roleName);
        var updatedRoleName = $"{roleName}_Updated";

        // Act
        var response = await client.PutAsJsonAsync($"/api/roles/{role.Id}", new { name = updatedRoleName, description = "After update" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateRoleResponse>();
        body!.Id.Should().Be(role.Id);
        var updatedRole = await GetRoleByNameAsync(client, updatedRoleName);
        updatedRole.Description.Should().Be("After update");
    }

    [Fact]
    public async Task DeleteRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/roles", new { name = roleName, description = "Will be deleted" });
        var role = await GetRoleByNameAsync(client, roleName);

        // Act
        var response = await client.DeleteAsync($"/api/roles/{role.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteRoleResponse>();
        body!.Success.Should().BeTrue();
        var rolesResponse = await client.GetAsync("/api/roles");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles.Should().NotBeNull();
        roles!.Items.Should().NotContain(x => x.Id == role.Id);
    }

    private static async Task<RoleResponse> GetRoleByNameAsync(HttpClient client, string roleName)
    {
        var rolesResponse = await client.GetAsync("/api/roles");
        rolesResponse.EnsureSuccessStatusCode();
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles.Should().NotBeNull();
        var role = roles!.Items.FirstOrDefault(x => x.Name == roleName);
        role.Should().NotBeNull();
        return role!;
    }
}
