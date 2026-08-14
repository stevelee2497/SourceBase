using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.HabitLogs;
using SourceBase.Application.Features.Habits;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Habits;

[EndpointFact(
    Feature = "Habits",
    Name = "Get Habits",
    Route = "GET /api/habits",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve my habits along with their log counts, so that I can see which habits I've been keeping up with and in what order.",
    Description = new[]
    {
        "Client calls the endpoint with a valid access token.",
        "Returns the authenticated user's habits, each including a computed `LogCount` (number of associated habit logs).",
        "Optional `from`/`to` query parameters restrict `LogCount` to logs whose `OccurredAt` falls inside the range (e.g. today only).",
        "Habits are ordered by `LogCount` descending then by `Name`, so the most-logged habits appear first.",
    })]
public class GetHabitsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "HABITS-GET-001: missing token returns 401")]
    public async Task GetHabits_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetHabitsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "HABITS-GET-002: created habit with no logs is returned")]
    public async Task GetHabits_WithNoLogs_ReturnsCreatedHabit()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"habits_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Morning Run", icon = "🏃" });

        // Act
        var response = await client.GetAsync(GetHabitsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var habits = await response.Content.ReadFromJsonAsync<List<HabitResponse>>();
        habits!.ShouldContain(h => h.Name == "Morning Run" && h.LogCount == 0);
    }

    [Fact(DisplayName = "HABITS-GET-003: habit log count is calculated correctly")]
    public async Task GetHabits_WithLogs_ReturnsCorrectLogCount()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"habits_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Drink Water", icon = "💧" });
        var created = await createRes.Content.ReadFromJsonAsync<CreateHabitResponse>();
        var habitId = created!.Id.ToString();

        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { habitId, habitName = "Drink Water", action = "HabitStarted", occurredAt = DateTime.UtcNow },
                new { habitId, habitName = "Drink Water", action = "HabitStarted", occurredAt = DateTime.UtcNow },
            }
        });

        // Act
        var response = await client.GetAsync(GetHabitsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var habits = await response.Content.ReadFromJsonAsync<List<HabitResponse>>();
        habits!.ShouldContain(h => h.Name == "Drink Water" && h.LogCount == 2);
    }

    [Fact(DisplayName = "HABITS-GET-004: habits are ordered by log count descending")]
    public async Task GetHabits_OrderedByLogCountDescending()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"habits_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var walkRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Walk", icon = "🚶" });
        var readRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Read", icon = "📚" });
        var walkId = (await walkRes.Content.ReadFromJsonAsync<CreateHabitResponse>())!.Id.ToString();
        var readId = (await readRes.Content.ReadFromJsonAsync<CreateHabitResponse>())!.Id.ToString();

        // Walk gets 1 log, Read gets 3 — Read should rank first
        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { habitId = walkId, habitName = "Walk", action = "HabitStarted", occurredAt = DateTime.UtcNow },
            }
        });
        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { habitId = readId, habitName = "Read", action = "HabitStarted", occurredAt = DateTime.UtcNow },
                new { habitId = readId, habitName = "Read", action = "HabitStarted", occurredAt = DateTime.UtcNow },
                new { habitId = readId, habitName = "Read", action = "HabitStarted", occurredAt = DateTime.UtcNow },
            }
        });

        // Act
        var response = await client.GetAsync(GetHabitsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var habits = await response.Content.ReadFromJsonAsync<List<HabitResponse>>();
        var userHabits = habits!.Where(h => !h.IsSystem).ToList();
        userHabits[0].Name.ShouldBe("Read");
        userHabits[0].LogCount.ShouldBe(3);
        userHabits[1].Name.ShouldBe("Walk");
        userHabits[1].LogCount.ShouldBe(1);
    }

    [Fact(DisplayName = "HABITS-GET-005: from/to range counts only logs inside the range")]
    public async Task GetHabits_WithDateRange_CountsOnlyLogsInRange()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"habits_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createRes = await client.PostAsJsonAsync(CreateHabitEndpoint.Route, new { name = "Stretch", icon = "🧘" });
        var habitId = (await createRes.Content.ReadFromJsonAsync<CreateHabitResponse>())!.Id.ToString();

        var today = DateTime.UtcNow;
        await client.PostAsJsonAsync(CreateHabitLogsEndpoint.Route, new
        {
            entries = new[]
            {
                new { habitId, habitName = "Stretch", action = "HabitStarted", occurredAt = today },
                new { habitId, habitName = "Stretch", action = "HabitStarted", occurredAt = today.AddDays(-3) },
            }
        });

        var from = today.Date;
        var to = today.Date.AddDays(1).AddTicks(-1);

        // Act
        var response = await client.GetAsync($"{GetHabitsEndpoint.Route}?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var habits = await response.Content.ReadFromJsonAsync<List<HabitResponse>>();
        habits!.ShouldContain(h => h.Name == "Stretch" && h.LogCount == 1);
    }
}
