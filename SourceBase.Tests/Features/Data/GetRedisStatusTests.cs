using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Data;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

public class GetRedisStatusTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{

    [Fact(DisplayName = "REDIS-STATUS-001: without token returns 401")]
    public async Task GetRedisStatus_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetRedisStatusEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [RequiresRedisFact(DisplayName = "REDIS-STATUS-002: redis container running returns online status true")]
    public async Task GetRedisStatus_WhenRedisContainerIsRunning_ReturnsIsOnlineTrue()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetRedisStatusEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetRedisStatusResponse>();
        body.ShouldNotBeNull();
        body!.IsOnline.ShouldBeTrue();
    }
}
