using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.TimeSheets;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TimeSheets;

[EndpointFact(
    Feature = "TimeSheets",
    Name = "List Time Sheets",
    Route = "GET /api/time-sheets",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve my time entries for a given date or month, so that I can review or manage my logged hours.",
    Description = new[]
    {
        "Client may filter by `date` (exact day), `year`, or `month` query parameters.",
        "Returns only entries belonging to the authenticated user — other users' entries are never included.",
        "Supports pagination via `page` and `limit` query parameters.",
    })]
public class GetTimeSheetsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TIMESHEET-GET-ALL-001: GetTimeSheets_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTimeSheets_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTimeSheetsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TIMESHEET-GET-ALL-002: GetTimeSheets_Authenticated_ReturnsOk")]
    public async Task GetTimeSheets_Authenticated_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTimeSheetsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
    }

    [Fact(DisplayName = "TIMESHEET-GET-ALL-003: GetTimeSheets_WithYearAndMonthFilter_ReturnsMatchingItems")]
    public async Task GetTimeSheets_WithYearAndMonthFilter_ReturnsMatchingItems()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"filter_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var matchingResponse = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-15", project = "JuneProject", hours = 8 } }
        });
        var matchingBody = await matchingResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-07-15", project = "JulyProject", hours = 8 } }
        });

        // Act
        var response = await client.GetAsync($"{GetTimeSheetsEndpoint.Route}?year=2025&month=6");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body!.Items.ShouldContain(x => x.Id == matchingBody!.Ids[0]);
        body.Items.ShouldNotContain(x => x.Project == "JulyProject");
    }

    [Fact(DisplayName = "TIMESHEET-GET-ALL-004: GetTimeSheets_WithDateFilter_ReturnsMatchingItems")]
    public async Task GetTimeSheets_WithDateFilter_ReturnsMatchingItems()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"datefilter_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var matchingResponse = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-10", project = "DayProject", hours = 6 } }
        });
        var matchingBody = await matchingResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-11", project = "OtherDayProject", hours = 5 } }
        });

        // Act
        var response = await client.GetAsync($"{GetTimeSheetsEndpoint.Route}?date=2025-06-10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body!.Items.ShouldContain(x => x.Id == matchingBody!.Ids[0]);
        body.Items.ShouldNotContain(x => x.Project == "OtherDayProject");
    }

    [Fact(DisplayName = "TIMESHEET-GET-ALL-005: GetTimeSheets_WithMultipleUsers_ReturnsOnlyCurrentUsersItems")]
    public async Task GetTimeSheets_WithMultipleUsers_ReturnsOnlyCurrentUsersItems()
    {
        // Arrange
        var firstClient = await factory.CreateAuthorizedClient($"ts_first_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var secondClient = await factory.CreateAuthorizedClient($"ts_second_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownResponse = await firstClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-10-01", project = "MyProject", hours = 8 } }
        });
        var ownBody = await ownResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        await secondClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-10-01", project = "OtherProject", hours = 8 } }
        });

        // Act
        var response = await firstClient.GetAsync(GetTimeSheetsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body!.Items.ShouldContain(x => x.Id == ownBody!.Ids[0]);
        body.Items.ShouldNotContain(x => x.Project == "OtherProject");
    }
}
