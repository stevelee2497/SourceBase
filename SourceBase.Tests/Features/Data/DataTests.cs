
using FluentAssertions;
using SourceBase.Api.Features.Data;
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
}