using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Data;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

public class GetStatsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "DATA-STATS-001: GetStats_WithoutToken_ReturnsUnauthorized")]
    public async Task GetStats_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetStatsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DATA-STATS-002: GetStats_AsAuthenticatedUser_ReturnsOk")]
    public async Task GetStats_AsAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetStatsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetStatsResponse>();
        body.Should().NotBeNull();
        body!.UserCount.Should().BeGreaterThan(0);
        body.TotalTodoItems.Should().BeGreaterThanOrEqualTo(0);
        body.CompletedTodoItems.Should().BeGreaterThanOrEqualTo(0);
        body.TotalTodoLists.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact(DisplayName = "DATA-STATS-003: GetStats_CompletedTodoItems_DoesNotExceedTotal")]
    public async Task GetStats_CompletedTodoItems_DoesNotExceedTotal()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetStatsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetStatsResponse>();
        body!.CompletedTodoItems.Should().BeLessThanOrEqualTo(body.TotalTodoItems);
    }
}
