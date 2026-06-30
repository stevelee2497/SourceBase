using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.HabitLogs;
using SourceBase.Application.Features.Habits;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Habits;

public class GetHabitsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "HABITS-GET-001: GetHabits_WithoutToken_ReturnsUnauthorized")]
    public async Task GetHabits_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetHabitsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "HABITS-GET-002: GetHabits_WithNoLogs_ReturnsCreatedHabit")]
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

    [Fact(DisplayName = "HABITS-GET-003: GetHabits_WithLogs_ReturnsCorrectLogCount")]
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

    [Fact(DisplayName = "HABITS-GET-004: GetHabits_OrderedByLogCountDescending")]
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
}
