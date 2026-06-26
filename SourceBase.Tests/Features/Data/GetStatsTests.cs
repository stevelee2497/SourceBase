using System.Net;
using System.Net.Http.Json;
using Shouldly;
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
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DATA-STATS-002: GetStats_AsAuthenticatedUser_ReturnsOk")]
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
        body.TotalTodoItems.ShouldBeGreaterThanOrEqualTo(0);
        body.CompletedTodoItems.ShouldBeGreaterThanOrEqualTo(0);
        body.TotalTodoLists.ShouldBeGreaterThanOrEqualTo(0);
        body.TotalBalance.ShouldBeGreaterThanOrEqualTo(0);
        body.MonthlyIncome.ShouldBeGreaterThanOrEqualTo(0);
        body.MonthlyExpense.ShouldBeGreaterThanOrEqualTo(0);
        body.LogTimeDetail.ShouldNotBeNull();
    }

    [Fact(DisplayName = "DATA-STATS-003: GetStats_CompletedTodoItems_DoesNotExceedTotal")]
    public async Task GetStats_CompletedTodoItems_DoesNotExceedTotal()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetStatsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetStatsResponse>();
        body!.CompletedTodoItems.ShouldBeLessThanOrEqualTo(body.TotalTodoItems);
    }
}
