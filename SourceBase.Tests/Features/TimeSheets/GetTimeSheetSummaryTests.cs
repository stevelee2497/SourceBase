using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.TimeSheets;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TimeSheets;

public class GetTimeSheetSummaryTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TIMESHEET-SUMMARY-001: GetTimeSheetSummary_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTimeSheetSummary_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{GetTimeSheetSummaryEndpoint.Route}?year=2025&month=6");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TIMESHEET-SUMMARY-002: GetTimeSheetSummary_WithValidMonthAndEntries_ReturnsAggregates")]
    public async Task GetTimeSheetSummary_WithValidMonthAndEntries_ReturnsAggregates()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[]
            {
                new { date = "2025-06-05", project = "Alpha", hours = 4.0 },
                new { date = "2025-06-05", project = "Beta", hours = 3.5 },
                new { date = "2025-06-10", project = "Alpha", hours = 8.0 },
            }
        });

        // Act
        var response = await client.GetAsync($"{GetTimeSheetSummaryEndpoint.Route}?year=2025&month=6");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTimeSheetSummaryResponse>();
        body!.Days.Should().HaveCount(2);

        var june5 = body.Days.Single(d => d.Date == new DateOnly(2025, 6, 5));
        june5.TotalHours.Should().Be(7.5m);
        june5.Projects.Should().BeEquivalentTo(["Alpha", "Beta"]);

        var june10 = body.Days.Single(d => d.Date == new DateOnly(2025, 6, 10));
        june10.TotalHours.Should().Be(8m);
        june10.Projects.Should().BeEquivalentTo(["Alpha"]);
    }

    [Fact(DisplayName = "TIMESHEET-SUMMARY-003: GetTimeSheetSummary_WithEmptyMonth_ReturnsEmptyList")]
    public async Task GetTimeSheetSummary_WithEmptyMonth_ReturnsEmptyList()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"empty_summary_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync($"{GetTimeSheetSummaryEndpoint.Route}?year=2099&month=12");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTimeSheetSummaryResponse>();
        body!.Days.Should().BeEmpty();
    }

    [Fact(DisplayName = "TIMESHEET-SUMMARY-004: GetTimeSheetSummary_WithInvalidMonth_ReturnsBadRequest")]
    public async Task GetTimeSheetSummary_WithInvalidMonth_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync($"{GetTimeSheetSummaryEndpoint.Route}?year=2025&month=13");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TIMESHEET-SUMMARY-005: GetTimeSheetSummary_OnlyReturnsCurrentUsersData")]
    public async Task GetTimeSheetSummary_OnlyReturnsCurrentUsersData()
    {
        // Arrange
        var firstClient = await factory.CreateAuthorizedClient($"sum_first_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var secondClient = await factory.CreateAuthorizedClient($"sum_second_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await firstClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-03", project = "MyWork", hours = 8 } }
        });
        await secondClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-03", project = "OtherWork", hours = 8 } }
        });

        // Act
        var response = await firstClient.GetAsync($"{GetTimeSheetSummaryEndpoint.Route}?year=2025&month=6");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTimeSheetSummaryResponse>();
        var june3 = body!.Days.FirstOrDefault(d => d.Date == new DateOnly(2025, 6, 3));
        june3.Should().NotBeNull();
        june3!.Projects.Should().Contain("MyWork");
        june3.Projects.Should().NotContain("OtherWork");
    }
}
