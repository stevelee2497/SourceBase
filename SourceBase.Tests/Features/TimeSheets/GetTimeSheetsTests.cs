using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.TimeSheets;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TimeSheets;

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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TIMESHEET-GET-ALL-002: GetTimeSheets_Authenticated_ReturnsOk")]
    public async Task GetTimeSheets_Authenticated_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTimeSheetsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == matchingBody!.Ids[0]);
        body.Items.Should().NotContain(x => x.Project == "JulyProject");
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body!.Items.Should().ContainSingle(x => x.Id == matchingBody!.Ids[0]);
        body.Items.Should().NotContain(x => x.Project == "OtherDayProject");
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTimeSheetResponse>>();
        body!.Items.Should().Contain(x => x.Id == ownBody!.Ids[0]);
        body.Items.Should().NotContain(x => x.Project == "OtherProject");
    }
}
