using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Data;
using SourceBase.Api.Features.Roles;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

public class GetEnumsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetEnumsResponse>();
        body.Should().NotBeNull();
        body!.Data.Should().HaveCount(1);
        body.Data[AvailableEnums.TodoItemStatus].Should().Contain(x => x.Name == TodoItemStatus.Open.ToString());
        body.Data[AvailableEnums.TodoItemStatus].Should().Contain(x => x.Name == TodoItemStatus.Completed.ToString());
        body.Data[AvailableEnums.TodoItemStatus].Should().Contain(x => x.Name == TodoItemStatus.Archived.ToString());
    }

    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetEnumsResponse>();
        body.Should().NotBeNull();
        body!.Data.Should().HaveCount(1);
        body.Data[AvailableEnums.Roles].Should().Contain(x => x.Name == roleName && x.Description == "Dynamic role");
    }

    [Fact]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
