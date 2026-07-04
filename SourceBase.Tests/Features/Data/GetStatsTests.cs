using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Data;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

[EndpointFact(
    Feature = "Data",
    Name = "Get Stats",
    Route = "GET /api/data/stats",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to view application-wide statistics, so that I can get a quick overview of users, tasks, and completion rates.",
    Description = new[]
    {
        "Client calls the endpoint with a valid access token.",
        "Returns four aggregate counts from the database:",
        "`userCount` — total number of registered users",
        "`totalTodoLists` — total number of todo lists",
        "`totalTodoItems` — total number of todo items",
        "`completedTodoItems` — count of items with status `Completed`",
    })]
public class GetStatsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "DATA-STATS-001: missing token returns 401")]
    public async Task GetStats_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetStatsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DATA-STATS-002: authenticated user returns 200")]
    public async Task GetStats_AsAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetStatsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetStatsResponse>();
        body.ShouldNotBeNull();
        body!.UserCount.ShouldBeGreaterThan(0);
        body.TotalBalance.ShouldBeGreaterThanOrEqualTo(0);
        body.MonthlyIncome.ShouldBeGreaterThanOrEqualTo(0);
        body.MonthlyExpense.ShouldBeGreaterThanOrEqualTo(0);
        body.LogTimeDetail.ShouldNotBeNull();
    }
}
