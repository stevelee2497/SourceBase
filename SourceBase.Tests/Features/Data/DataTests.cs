
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Data;
using SourceBase.Api.Features.Roles;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

public class DataTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task Database_IsCreatedAndSeeded()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/roles");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<RoleResponse>>();

        // Assert
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body!.Items.Should().ContainSingle(role => role.Name == "Admin");
        body!.Items.Should().ContainSingle(role => role.Name == "User");
    }

    [Fact]
    public async Task GetEnums_WithRequestedStaticEnums_ReturnsOnlyRequestedDefinitions()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var request = new GetEnumsRequest([AvailableEnums.TodoItemStatus]);

        // Act
        var response = await client.PostAsJsonAsync("/api/data/enums", request);

        // Assert
        response.EnsureSuccessStatusCode();
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
        var client = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";
        var description = "Default Admin Role";
        await client.PostAsJsonAsync("/api/roles", new { name = roleName, description });
        var request = new GetEnumsRequest([AvailableEnums.Roles]);

        // Act
        var response = await client.PostAsJsonAsync("/api/data/enums", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<GetEnumsResponse>();
        body.Should().NotBeNull();
        body!.Data.Should().HaveCount(1);
        body.Data[AvailableEnums.Roles].Should().Contain(x => x.Name == roleName && x.Description == description);
    }
}