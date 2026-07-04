using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.HabitLogs;
using SourceBase.Application.Features.Habits;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.HabitLogs;

[EndpointFact(
    Feature = "HabitLogs",
    Name = "Create Habit Logs",
    Route = "POST /api/habit-logs",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to record one or more habit log entries in a single batch, so that I can track habit-related events (started, dismissed, snoozed, etc.) without a separate request per entry.",
    Description = new[]
    {
        "Client sends `entries` (required, non-empty array), each with `action` (required), `occurredAt` (required), and optional `habitId`/`habitName`.",
        "If `entries` is empty, or any entry is missing `occurredAt`, the request returns `400 Bad Request`.",
        "Each log entry is created and associated with the authenticated user.",
        "Returns the new log entries' `Ids` (one per submitted entry, in order).",
    })]
public class CreateHabitLogsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "HLOG-CREATE-001: CreateHabitLogs_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateHabitLogs_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[] { new { action = "HabitStarted", occurredAt = DateTime.UtcNow } }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "HLOG-CREATE-002: CreateHabitLogs_WithSingleEntry_ReturnsOkWithId")]
    public async Task CreateHabitLogs_WithSingleEntry_ReturnsOkWithId()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var habitRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Short Walk", icon = "🚶" });
        var habit = await habitRes.Content.ReadFromJsonAsync<CreateHabitResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { habitId = habit!.Id, habitName = "Short Walk", action = "HabitStarted", occurredAt = DateTime.UtcNow }
            }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateHabitLogsResponse>();
        body!.Ids.ShouldHaveSingleItem();
        body.Ids[0].ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "HLOG-CREATE-003: CreateHabitLogs_WithMultipleEntries_ReturnsBatchIds")]
    public async Task CreateHabitLogs_WithMultipleEntries_ReturnsBatchIds()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var now = DateTime.UtcNow;
        var walkRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Short Walk", icon = "🚶" });
        var drinkRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Drink Water", icon = "💧" });
        var walkId = (await walkRes.Content.ReadFromJsonAsync<CreateHabitResponse>())!.Id;
        var drinkId = (await drinkRes.Content.ReadFromJsonAsync<CreateHabitResponse>())!.Id;

        // Act
        var response = await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new object[]
            {
                new { habitId = walkId,  habitName = "Short Walk",  action = "HabitStarted", occurredAt = now },
                new { habitId = drinkId, habitName = "Drink Water", action = "HabitStarted", occurredAt = now },
                new { action = "Dismissed", occurredAt = now },
            }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateHabitLogsResponse>();
        body!.Ids.Count.ShouldBe(3);
        body.Ids.ShouldAllBe(id => id != Guid.Empty);
    }

    [Fact(DisplayName = "HLOG-CREATE-004: CreateHabitLogs_WithEmptyEntries_ReturnsBadRequest")]
    public async Task CreateHabitLogs_WithEmptyEntries_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = Array.Empty<object>()
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "HLOG-CREATE-005: CreateHabitLogs_WithMissingOccurredAt_ReturnsBadRequest")]
    public async Task CreateHabitLogs_WithMissingOccurredAt_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act — DateTime.MinValue is treated as empty by NotEmpty() validator
        var response = await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[] { new { action = "HabitStarted", occurredAt = DateTime.MinValue } }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "HLOG-CREATE-006: CreateHabitLogs_WithAllActionTypes_ReturnsOk")]
    public async Task CreateHabitLogs_WithAllActionTypes_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var now = DateTime.UtcNow;

        // Act
        var response = await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { action = "HabitStarted",    occurredAt = now },
                new { action = "Dismissed",        occurredAt = now },
                new { action = "Snoozed",          occurredAt = now },
                new { action = "SuppressedVideo",  occurredAt = now },
            }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateHabitLogsResponse>();
        body!.Ids.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "HLOG-CREATE-007: CreateHabitLogs_ScopedToCurrentUser_NotVisibleToOtherUser")]
    public async Task CreateHabitLogs_ScopedToCurrentUser_NotVisibleToOtherUser()
    {
        // Arrange
        var userA = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var userB = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await userA.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[] { new { action = "Dismissed", occurredAt = DateTime.UtcNow } }
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateHabitLogsResponse>();
        var createdId = createBody!.Ids[0];

        // Act — userB fetches their own logs (should be empty)
        var response = await userB.GetAsync(GetHabitLogsEndpoint.Route);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.ShouldNotContain(l => l.Id == createdId);
    }
}
