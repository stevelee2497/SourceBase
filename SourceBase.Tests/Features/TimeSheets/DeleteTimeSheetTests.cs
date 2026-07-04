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
    Name = "Delete Time Sheet",
    Route = "DELETE /api/time-sheets/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to delete a time entry I no longer need, so that my records remain accurate.",
    Description = new[]
    {
        "Client provides the entry `id` as a route parameter.",
        "If the entry does not exist or belongs to a different user → `404 Not Found`.",
        "The entry is permanently removed from the database.",
    })]
public class DeleteTimeSheetTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TIMESHEET-DELETE-001: DeleteTimeSheet_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteTimeSheet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteTimeSheetEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TIMESHEET-DELETE-002: DeleteTimeSheet_ExistingEntry_ReturnsOk")]
    public async Task DeleteTimeSheet_ExistingEntry_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"ts_del_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-25", project = "ToDelete", hours = 4 } }
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTimeSheetEndpoint.Route.WithId(createBody!.Ids[0]));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteTimeSheetResponse>();
        body!.Success.ShouldBeTrue();

        var getResponse = await client.GetAsync(GetTimeSheetEndpoint.Route.WithId(createBody.Ids[0]));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TIMESHEET-DELETE-003: DeleteTimeSheet_WithNonExistentId_ReturnsNotFound")]
    public async Task DeleteTimeSheet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteTimeSheetEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TIMESHEET-DELETE-004: DeleteTimeSheet_WithOtherUsersEntry_ReturnsNotFound")]
    public async Task DeleteTimeSheet_WithOtherUsersEntry_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"ts_del_owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var strangerClient = await factory.CreateAuthorizedClient($"ts_del_stranger_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-12-01", project = "OwnerEntry", hours = 8 } }
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        // Act
        var response = await strangerClient.DeleteAsync(DeleteTimeSheetEndpoint.Route.WithId(createBody!.Ids[0]));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
