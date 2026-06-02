using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.TimeSheets;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TimeSheets;

public class CreateTimeSheetTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TIMESHEET-CREATE-001: CreateTimeSheet_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateTimeSheet_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-01", project = "ProjectA", hours = 8 } }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-002: CreateTimeSheet_WithValidData_ReturnsOk")]
    public async Task CreateTimeSheet_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-01", project = "ProjectA", hours = 8 } }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();
        body!.Ids.Should().HaveCount(1);
        body.Ids[0].Should().NotBeEmpty();
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-003: CreateTimeSheet_WithExistingDateAndProject_UpdatesHours")]
    public async Task CreateTimeSheet_WithExistingDateAndProject_UpdatesHours()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"upsert_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var firstResponse = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-07-01", project = "UpsertProject", hours = 4 } }
        });
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        // Act
        var secondResponse = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-07-01", project = "UpsertProject", hours = 8 } }
        });
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondBody!.Ids[0].Should().Be(firstBody!.Ids[0]);

        var entity = await factory.WithDbContextAsync(db => db.TimeSheets.SingleAsync(x => x.Id == firstBody.Ids[0]));
        entity.Hours.Should().Be(8);
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-004: CreateTimeSheet_WithMultipleItems_CreatesAll")]
    public async Task CreateTimeSheet_WithMultipleItems_CreatesAll()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"multi_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[]
            {
                new { date = "2025-08-01", project = "Alpha", hours = 4.0 },
                new { date = "2025-08-01", project = "Beta", hours = 3.5 },
            }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTimeSheetResponse>();
        body!.Ids.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-005: CreateTimeSheet_WithMissingProject_ReturnsBadRequest")]
    public async Task CreateTimeSheet_WithMissingProject_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-01", project = "", hours = 8 } }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-006: CreateTimeSheet_WithZeroHours_ReturnsBadRequest")]
    public async Task CreateTimeSheet_WithZeroHours_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-01", project = "ProjectA", hours = 0 } }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-007: CreateTimeSheet_WithHoursExceeding24_ReturnsBadRequest")]
    public async Task CreateTimeSheet_WithHoursExceeding24_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-06-01", project = "ProjectA", hours = 25 } }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-008: CreateTimeSheet_WithEmptyItems_ReturnsBadRequest")]
    public async Task CreateTimeSheet_WithEmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = Array.Empty<object>()
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TIMESHEET-CREATE-009: CreateTimeSheet_UpsertDoesNotOverwriteOtherUsersEntry")]
    public async Task CreateTimeSheet_UpsertDoesNotOverwriteOtherUsersEntry()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var strangerClient = await factory.CreateAuthorizedClient($"stranger_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await ownerClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-09-01", project = "SharedName", hours = 8 } }
        });

        // Act — stranger posts the same date+project: should create a new entry, not update owner's
        var response = await strangerClient.PostAsJsonAsync(CreateTimeSheetEndpoint.Route, new
        {
            items = new[] { new { date = "2025-09-01", project = "SharedName", hours = 4 } }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entriesForDate = await factory.WithDbContextAsync(db =>
            db.TimeSheets.Where(x => x.Date == new DateOnly(2025, 9, 1) && x.Project == "SharedName").ToListAsync());
        entriesForDate.Should().HaveCount(2);
    }
}
