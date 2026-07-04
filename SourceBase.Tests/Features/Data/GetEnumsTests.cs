using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Data;
using SourceBase.Application.Features.Roles;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

[EndpointFact(
    Feature = "Data",
    Name = "Get Enums",
    Route = "POST /api/data/enums",
    Auth = "Anonymous",
    UseCase = "As a client application, I want to fetch the definitions of one or more enum types in a single request, so that I can populate dropdowns and labels without hard-coding values.",
    Description = new[]
    {
        "Client sends a list of `enums` (e.g. `[\"TodoItemStatus\", \"Roles\"]`).",
        "The list must not be empty → `400 Bad Request` if empty.",
        "Static enum types (`RolesOrder`, `TodoItemStatus`) are resolved from the .NET enum values.",
        "The special `Roles` enum type is resolved dynamically from the database, returning the current list of roles.",
        "Returns a dictionary keyed by enum type, each containing a list of `{ name, description }` entries.",
    })]
public class GetEnumsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "DATA-ENUMS-001: GetEnums_WithRequestedStaticEnums_ReturnsOnlyRequestedDefinitions")]
    public async Task GetEnums_WithRequestedStaticEnums_ReturnsOnlyRequestedDefinitions()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(GetEnumsEndpoint.Route, new
        {
            enums = new[] { AvailableEnums.TodoItemStatus },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetEnumsResponse>();
        body.ShouldNotBeNull();
        body!.Data.Count.ShouldBe(1);
        body.Data[AvailableEnums.TodoItemStatus].ShouldContain(x => x.Name == TodoItemStatus.Open.ToString());
        body.Data[AvailableEnums.TodoItemStatus].ShouldContain(x => x.Name == TodoItemStatus.Completed.ToString());
        body.Data[AvailableEnums.TodoItemStatus].ShouldContain(x => x.Name == TodoItemStatus.Archived.ToString());
    }

    [Fact(DisplayName = "DATA-ENUMS-002: GetEnums_WithRolesRequested_ReturnsRolesFromDatabase")]
    public async Task GetEnums_WithRolesRequested_ReturnsRolesFromDatabase()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var roleName = $"Role_{Guid.NewGuid():N}";

        await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = roleName,
            description = "Dynamic role",
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(GetEnumsEndpoint.Route, new
        {
            enums = new[] { AvailableEnums.Roles },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetEnumsResponse>();
        body.ShouldNotBeNull();
        body!.Data.Count.ShouldBe(1);
        body.Data[AvailableEnums.Roles].ShouldContain(x => x.Name == roleName && x.Description == "Dynamic role");
    }

    [Fact(DisplayName = "DATA-ENUMS-003: GetEnums_WithEmptyEnums_ReturnsBadRequest")]
    public async Task GetEnums_WithEmptyEnums_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(GetEnumsEndpoint.Route, new
        {
            enums = Array.Empty<AvailableEnums>(),
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
