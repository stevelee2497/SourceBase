using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.HabitLogs;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.HabitLogs;

public class DeleteHabitLogTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "HLOG-DELETE-001: DeleteHabitLog_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteHabitLog_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteHabitLogEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "HLOG-DELETE-002: DeleteHabitLog_WithValidId_ReturnsOkAndRemovesLog")]
    public async Task DeleteHabitLog_WithValidId_ReturnsOkAndRemovesLog()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[] { new { action = "Dismissed", occurredAt = DateTime.UtcNow } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateHabitLogsResponse>();
        var id = created!.Ids[0];

        // Act
        var deleteResponse = await client.DeleteAsync(DeleteHabitLogEndpoint.Route.WithId(id));

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await deleteResponse.Content.ReadFromJsonAsync<DeleteHabitLogResponse>();
        body!.Success.ShouldBeTrue();

        // Confirm log is gone
        var listResponse = await client.GetAsync(GetHabitLogsEndpoint.Route);
        var list = await listResponse.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        list!.Items.ShouldNotContain(l => l.Id == id);
    }

    [Fact(DisplayName = "HLOG-DELETE-003: DeleteHabitLog_WithUnknownId_ReturnsNotFound")]
    public async Task DeleteHabitLog_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.DeleteAsync(DeleteHabitLogEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "HLOG-DELETE-004: DeleteHabitLog_WithOtherUsersLog_ReturnsNotFound")]
    public async Task DeleteHabitLog_WithOtherUsersLog_ReturnsNotFound()
    {
        // Arrange
        var owner = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var other = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await owner.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[] { new { action = "Snoozed", occurredAt = DateTime.UtcNow } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateHabitLogsResponse>();
        var id = created!.Ids[0];

        // Act — different user tries to delete owner's log
        var response = await other.DeleteAsync(DeleteHabitLogEndpoint.Route.WithId(id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
