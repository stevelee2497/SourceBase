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
    Name = "Get Time Sheet",
    Route = "GET /api/time-sheets/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve the details of a specific time entry by its ID, so that I can inspect or edit it.",
    Description = new[]
    {
        "Client provides the entry `id` as a route parameter.",
        "If the entry does not exist or belongs to a different user → `404 Not Found`.",
        "Returns the full entry: `id`, `date`, `project`, `hours`, and audit fields.",
    })]
public class GetTimeSheetTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TIMESHEET-GET-001: missing token returns 401")]
    public async Task GetTimeSheet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTimeSheetEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TIMESHEET-GET-002: valid id returns 200 and correct data")]
    public async Task GetTimeSheet_WithValidId_ReturnsCorrectData()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"getsingle_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-20", project = "ProjectX", hours = 7.5 } }
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        // Act
        var response = await client.GetAsync(GetTimeSheetEndpoint.Route.WithId(createBody!.Ids[0]));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTimeSheetResponse>();
        body!.Id.ShouldBe(createBody.Ids[0]);
        body.Project.ShouldBe("ProjectX");
        body.Hours.ShouldBe(7.5m);
        body.Date.ShouldBe(new DateOnly(2025, 6, 20));
    }

    [Fact(DisplayName = "TIMESHEET-GET-003: non-existent id returns 404")]
    public async Task GetTimeSheet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTimeSheetEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TIMESHEET-GET-004: other user's id returns 404")]
    public async Task GetTimeSheet_WithOtherUsersId_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"ts_get_owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var strangerClient = await factory.CreateAuthorizedClient($"ts_get_stranger_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-11-01", project = "Private", hours = 8 } }
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        // Act
        var response = await strangerClient.GetAsync(GetTimeSheetEndpoint.Route.WithId(createBody!.Ids[0]));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
