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
    Name = "Get Roles",
    Route = "GET /api/roles",
    Auth = "Anonymous",
    UseCase = "As any client (authenticated or not), I want to retrieve the list of available roles with paging and ordering, so that I can populate dropdowns and role-assignment UIs.",
    Description = new[]
    {
        "Client sends optional paging parameters (`page`, `limit`, `order`, `orderBy`).",
        "Returns a paginated list of roles (`id`, `name`, `description`).",
    })]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        body.ShouldNotBeNull();
        body!.Items.ShouldContain(role => role.Name == "Admin");
        body.Items.ShouldContain(role => role.Name == "User");
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Items.ShouldContain(x => x.Name == roleName && x.Description == "Created role");
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();
        roles!.Page.ShouldBe(1);
        roles.Limit.ShouldBe(1);
        roles.Items.Count.ShouldBe(1);
        roles.Items.Single().Id.ShouldBe(expectedRoleBody!.Id);
    }
}
