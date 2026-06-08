using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Roles;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Roles;

public class CreateRoleTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ROLES-CREATE-001: CreateRole_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateRole_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "Created in integration test",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ROLES-CREATE-002: CreateRole_WithNonAdminUser_ReturnsForbidden")]
    public async Task CreateRole_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "Created in integration test",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "ROLES-CREATE-003: CreateRole_WithValidData_ReturnsOk")]
    public async Task CreateRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";

        // Act
        var response = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName,
            description = "Created in integration test",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateRoleResponse>();
        body!.Id.Should().NotBeEmpty();

        var rolesResponse = await client.GetAsync(GetRolesEndpoint.Route);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.Should().ContainSingle(x => x.Name == roleName && x.Description == "Created in integration test");
    }

    [Fact(DisplayName = "ROLES-CREATE-004: CreateRole_WithDuplicateNameIgnoringCase_ReturnsBadRequest")]
    public async Task CreateRole_WithDuplicateNameIgnoringCase_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"duplicate_{Guid.NewGuid():N}";

        await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName,
            description = "Role",
        });

        // Act
        var response = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName.ToUpperInvariant(),
            description = "Role",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ROLES-CREATE-005: CreateRole_WithWhitespaceAroundName_TrimsBeforePersisting")]
    public async Task CreateRole_WithWhitespaceAroundName_TrimsBeforePersisting()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var trimmedName = $"trimmed_{Guid.NewGuid():N}";

        // Act
        var response = await client.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"  {trimmedName}  ",
            description = "Trimmed role",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rolesResponse = await client.GetAsync(GetRolesEndpoint.Route);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.Should().Contain(x => x.Name == trimmedName);
    }
}
