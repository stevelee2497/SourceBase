using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Data;
using SourceBase.Application.Features.Roles;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

public class GetEnumsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "DATA-ENUMS-001: GetEnums_WithRequestedStaticEnums_ReturnsOnlyRequestedDefinitions")]
    public async Task GetEnums_WithRequestedStaticEnums_ReturnsOnlyRequestedDefinitions()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(GetEnumsEndpoint.Route, new
        {
            enums = new[] { AvailableEnums.TodoItemStatus },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetEnumsResponse>();
        body.ShouldNotBeNull();
        body!.Data.Count.ShouldBe(1);
        body.Data[AvailableEnums.TodoItemStatus].ShouldContain(x => x.Name == TodoItemStatus.Open.ToString());
        body.Data[AvailableEnums.TodoItemStatus].ShouldContain(x => x.Name == TodoItemStatus.Completed.ToString());
        body.Data[AvailableEnums.TodoItemStatus].ShouldContain(x => x.Name == TodoItemStatus.Archived.ToString());
    }

    [Fact(DisplayName = "DATA-ENUMS-002: GetEnums_WithRolesRequested_ReturnsRolesFromDatabase")]
    public async Task GetEnums_WithRolesRequested_ReturnsRolesFromDatabase()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";

        await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName,
            description = "Dynamic role",
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(GetEnumsEndpoint.Route, new
        {
            enums = new[] { AvailableEnums.Roles },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetEnumsResponse>();
        body.ShouldNotBeNull();
        body!.Data.Count.ShouldBe(1);
        body.Data[AvailableEnums.Roles].ShouldContain(x => x.Name == roleName && x.Description == "Dynamic role");
    }

    [Fact(DisplayName = "DATA-ENUMS-003: GetEnums_WithEmptyEnums_ReturnsBadRequest")]
    public async Task GetEnums_WithEmptyEnums_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(GetEnumsEndpoint.Route, new
        {
            enums = Array.Empty<AvailableEnums>(),
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
