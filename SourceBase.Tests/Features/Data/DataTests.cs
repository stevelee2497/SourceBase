
using FluentAssertions;
using SourceBase.Api.Features.Data;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

public class DataTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task Database_IsCreatedAndSeeded()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync("/api/roles");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<List<RoleResponse>>();

        // Assert
        body.Should().NotBeNull();
        body!.Should().ContainSingle(role => role.Name == "Admin");
        body!.Should().ContainSingle(role => role.Name == "User");
    }
}