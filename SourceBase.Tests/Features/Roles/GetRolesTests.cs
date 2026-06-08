using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Roles;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Roles;

public class GetRolesTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ROLES-GET-001: GetRoles_WithAnonymousClient_ReturnsSeededRoles")]
    public async Task GetRoles_WithAnonymousClient_ReturnsSeededRoles()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetRolesEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle(role => role.Name == "Admin");
        body.Items.Should().ContainSingle(role => role.Name == "User");
    }

    [Fact(DisplayName = "ROLES-GET-002: GetRoles_WithCreatedRole_ReturnsRole")]
    public async Task GetRoles_WithCreatedRole_ReturnsRole()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";

        await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName,
            description = "Created role",
        });
        var anonymousClient = factory.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync(GetRolesEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.Should().Contain(x => x.Name == roleName && x.Description == "Created role");
    }

    [Fact(DisplayName = "ROLES-GET-003: GetRoles_WithPagingAndOrdering_ReturnsRequestedPage")]
    public async Task GetRoles_WithPagingAndOrdering_ReturnsRequestedPage()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();

        await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = "AlphaRole",
            description = "First",
        });
        var expectedRoleResponse = await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = "ZuluRole",
            description = "Last",
        });
        var expectedRoleBody = await expectedRoleResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();

        // Act
        var response = await factory.CreateClient().GetAsync($"{GetRolesEndpoint.Route}?orderBy=Name&order=Desc&page=1&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Page.Should().Be(1);
        roles.Limit.Should().Be(1);
        roles.Items.Should().ContainSingle();
        roles.Items.Single().Id.Should().Be(expectedRoleBody!.Id);
    }
}
