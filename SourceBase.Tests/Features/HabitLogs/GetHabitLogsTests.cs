using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.HabitLogs;
using SourceBase.Application.Features.Habits;
using SourceBase.Application.Shared;
using SourceBase.Domain.Entities;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.HabitLogs;

[EndpointFact(
    Feature = "HabitLogs",
    Name = "Get Habit Logs",
    Route = "GET /api/habit-logs",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to list my habit log entries with filtering and pagination, so that I can review my habit activity history.",
    Description = new[]
    {
        "Client sends optional filters: `action`, `ignore` (actions to exclude), `from`/date range, plus paging parameters (`page`, `limit`).",
        "Returns only habit log entries belonging to the authenticated user.",
        "Each item includes `id`, `habitId`, `habitName`, `action`, and `occurredAt`.",
    })]
public class GetHabitLogsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "HLOG-GET-001: no token returns 401")]
    public async Task GetHabitLogs_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetHabitLogsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "HLOG-GET-002: no filters returns all logs for current user")]
    public async Task GetHabitLogs_WithNoFilters_ReturnsAllLogsForCurrentUser()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var now = DateTime.UtcNow;

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { action = "HabitStarted", occurredAt = now },
                new { action = "Snoozed",      occurredAt = now },
            }
        });

        // Act
        var response = await client.GetAsync(GetHabitLogsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        body!.Total.ShouldBe(2);
        body.Items.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "HLOG-GET-003: action filter returns only matching logs")]
    public async Task GetHabitLogs_FilteredByAction_ReturnsOnlyMatchingLogs()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var now = DateTime.UtcNow;

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { action = "HabitStarted", occurredAt = now },
                new { action = "HabitStarted", occurredAt = now },
                new { action = "Dismissed",    occurredAt = now },
            }
        });

        // Act
        var response = await client.GetAsync($"{GetHabitLogsEndpoint.Route}?action=HabitStarted");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        body!.Total.ShouldBe(2);
        body.Items.ShouldAllBe(l => l.Action == HabitLogAction.HabitStarted);
    }

    [Fact(DisplayName = "HLOG-GET-004: date range filter returns logs in range")]
    public async Task GetHabitLogs_FilteredByDateRange_ReturnsLogsInRange()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var old = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var recent = DateTime.UtcNow;

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { action = "Dismissed", occurredAt = old },
                new { action = "Snoozed",   occurredAt = recent },
            }
        });

        var cutoff = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var from = Uri.EscapeDataString(cutoff.ToString("o"));

        // Act — only logs on or after 2025-01-01 should appear
        var response = await client.GetAsync($"{GetHabitLogsEndpoint.Route}?from={from}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        body!.Items.ShouldAllBe(l => l.OccurredAt >= cutoff);
        body.Items.ShouldNotContain(l => l.OccurredAt < cutoff);
    }

    [Fact(DisplayName = "HLOG-GET-005: pagination respects limit and page")]
    public async Task GetHabitLogs_Paginated_RespectsLimitAndPage()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var now = DateTime.UtcNow;

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = Enumerable.Range(0, 5)
                .Select(_ => new { action = "Snoozed", occurredAt = now })
                .ToArray<object>()
        });

        // Act
        var page1Response = await client.GetAsync($"{GetHabitLogsEndpoint.Route}?page=1&limit=2");
        var page2Response = await client.GetAsync($"{GetHabitLogsEndpoint.Route}?page=2&limit=2");

        // Assert
        page1Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page2Response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page1 = await page1Response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();

        page1!.Items.Count.ShouldBe(2);
        page2!.Items.Count.ShouldBe(2);
        page1.Total.ShouldBe(5);
        page2.Total.ShouldBe(5);
    }

    [Fact(DisplayName = "HLOG-GET-006: does not return other users' logs")]
    public async Task GetHabitLogs_DoesNotReturnOtherUsersLogs()
    {
        // Arrange
        var userA = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var userB = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await userA.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[] { new { action = "Dismissed", occurredAt = DateTime.UtcNow } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateHabitLogsResponse>();
        var createdId = created!.Ids[0];

        // Act — userB should see an empty list
        var response = await userB.GetAsync(GetHabitLogsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        body!.Items.ShouldNotContain(l => l.Id == createdId);
    }

    [Fact(DisplayName = "HLOG-GET-008: ignore actions exclude matching logs")]
    public async Task GetHabitLogs_WithIgnoreActions_ExcludesMatchingLogs()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var now = DateTime.UtcNow;

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { action = "HabitStarted", occurredAt = now },
                new { action = "Dismissed",    occurredAt = now },
                new { action = "Snoozed",      occurredAt = now },
            }
        });

        // Act
        var response = await client.GetAsync($"{GetHabitLogsEndpoint.Route}?ignore=Dismissed");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        body!.Total.ShouldBe(2);
        body.Items.ShouldNotContain(l => l.Action == HabitLogAction.Dismissed);
    }

    [Fact(DisplayName = "HLOG-GET-007: returns correct fields")]
    public async Task GetHabitLogs_ReturnsCorrectFields()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var occurredAt = new DateTime(2025, 3, 15, 9, 30, 0, DateTimeKind.Utc);

        var habitRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Short Walk", icon = "🚶" });
        var habit = await habitRes.Content.ReadFromJsonAsync<CreateHabitResponse>();

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { habitId = habit!.Id, habitName = "Short Walk", action = "HabitStarted", occurredAt }
            }
        });

        // Act
        var response = await client.GetAsync(GetHabitLogsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        var log = body!.Items.Single();
        log.HabitId.ShouldBe(habit.Id);
        log.HabitName.ShouldBe("Short Walk");
        log.Action.ShouldBe(HabitLogAction.HabitStarted);
        log.OccurredAt.ShouldBe(occurredAt);
        log.Id.ShouldNotBe(Guid.Empty);
    }
}
