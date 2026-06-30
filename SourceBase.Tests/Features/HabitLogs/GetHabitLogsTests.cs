using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.HabitLogs;
using SourceBase.Application.Shared;
using SourceBase.Domain.Entities;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.HabitLogs;

public class GetHabitLogsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "HLOG-GET-001: GetHabitLogs_WithoutToken_ReturnsUnauthorized")]
    public async Task GetHabitLogs_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetHabitLogsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "HLOG-GET-002: GetHabitLogs_WithNoFilters_ReturnsAllLogsForCurrentUser")]
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

    [Fact(DisplayName = "HLOG-GET-003: GetHabitLogs_FilteredByAction_ReturnsOnlyMatchingLogs")]
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

    [Fact(DisplayName = "HLOG-GET-004: GetHabitLogs_FilteredByDateRange_ReturnsLogsInRange")]
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

    [Fact(DisplayName = "HLOG-GET-005: GetHabitLogs_Paginated_RespectsLimitAndPage")]
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

    [Fact(DisplayName = "HLOG-GET-006: GetHabitLogs_DoesNotReturnOtherUsersLogs")]
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

    [Fact(DisplayName = "HLOG-GET-008: GetHabitLogs_WithIgnoreActions_ExcludesMatchingLogs")]
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
        var response = await client.GetAsync($"{GetHabitLogsEndpoint.Route}?ignoreActions=Dismissed");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        body!.Total.ShouldBe(2);
        body.Items.ShouldNotContain(l => l.Action == HabitLogAction.Dismissed);
    }

    [Fact(DisplayName = "HLOG-GET-007: GetHabitLogs_ReturnsCorrectFields")]
    public async Task GetHabitLogs_ReturnsCorrectFields()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"hlog_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var occurredAt = new DateTime(2025, 3, 15, 9, 30, 0, DateTimeKind.Utc);

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { habitId = "walk", habitName = "Short Walk", action = "HabitStarted", occurredAt }
            }
        });

        // Act
        var response = await client.GetAsync(GetHabitLogsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetHabitLogResponse>>();
        var log = body!.Items.Single();
        log.HabitId.ShouldBe("walk");
        log.HabitName.ShouldBe("Short Walk");
        log.Action.ShouldBe(HabitLogAction.HabitStarted);
        log.OccurredAt.ShouldBe(occurredAt);
        log.Id.ShouldNotBe(Guid.Empty);
    }
}
