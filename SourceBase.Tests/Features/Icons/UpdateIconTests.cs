using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Icons;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Icons;

[EndpointFact(
    Feature = "Icons",
    Name = "Update Icon",
    Route = "PATCH /api/icons/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to partially update a custom icon's value, name, group, or sort order, so that I can correct or reorganise it without resending all fields.",
    Description = new[]
    {
        "Client sends the icon `id` (route) and any subset of: `value`, `name`, `group`, `sortOrder`. All fields are optional — only provided (non-null) fields are updated.",
        "If the icon doesn't exist → `404 Not Found`.",
        "If the icon is a system icon (`IsSystem = true`) → `403 Forbidden`.",
        "If `value` or `name` is provided but empty → `400 Bad Request`.",
        "Returns the updated icon's `Id`.",
    })]
public class UpdateIconTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ICONS-UPDATE-001: without token returns 401")]
    public async Task UpdateIcon_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateIconEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            value = "🦊",
            name = "Fox",
            group = "General",
            sortOrder = 1,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-UPDATE-002: valid request returns 200")]
    public async Task UpdateIcon_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var createResponse = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🦊",
            name = $"Fox_{Guid.NewGuid():N}",
            group = "General",
            sortOrder = 1,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateIconResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateIconEndpoint.Route.WithId(created!.Id), new
        {
            value = "🐺",
            name = "Wolf",
            group = "General",
            sortOrder = 2,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateIconResponse>();
        body!.Id.ShouldBe(created.Id);
    }

    [Fact(DisplayName = "ICONS-UPDATE-003: persists changes")]
    public async Task UpdateIcon_PersistsChanges()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var createResponse = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🦊",
            name = $"Fox_{Guid.NewGuid():N}",
            group = "Wallet",
            sortOrder = 50,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateIconResponse>();

        await client.PatchAsJsonAsync(UpdateIconEndpoint.Route.WithId(created!.Id), new
        {
            value = "🐺",
            name = "Wolf_Updated",
            group = "Category",
            sortOrder = 99,
        });

        // Act
        var allResponse = await client.GetAsync(GetIconsEndpoint.Route);
        var icons = await allResponse.Content.ReadFromJsonAsync<List<IconResponse>>();

        // Assert
        var updated = icons!.FirstOrDefault(i => i.Id == created.Id);
        updated.ShouldNotBeNull();
        updated!.Value.ShouldBe("🐺");
        updated.Name.ShouldBe("Wolf_Updated");
        updated.Group.ShouldBe("Category");
        updated.SortOrder.ShouldBe(99);
    }

    [Fact(DisplayName = "ICONS-UPDATE-004: unknown id returns 404")]
    public async Task UpdateIcon_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateIconEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            value = "🦊",
            name = "Fox",
            group = "General",
            sortOrder = 1,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "ICONS-UPDATE-005: system icon returns 403")]
    public async Task UpdateIcon_OnSystemIcon_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var iconsResponse = await client.GetAsync(GetIconsEndpoint.Route);
        var icons = await iconsResponse.Content.ReadFromJsonAsync<List<IconResponse>>();
        var systemIcon = icons!.First(i => i.IsSystem);

        // Act
        var response = await client.PatchAsJsonAsync(UpdateIconEndpoint.Route.WithId(systemIcon.Id), new
        {
            value = "❌",
            name = "Hacked",
            group = "General",
            sortOrder = 0,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "ICONS-UPDATE-006: empty value returns 400")]
    public async Task UpdateIcon_WithEmptyValue_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var createResponse = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🦊",
            name = $"Fox_{Guid.NewGuid():N}",
            group = "General",
            sortOrder = 1,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateIconResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateIconEndpoint.Route.WithId(created!.Id), new
        {
            value = "",
            name = "Fox",
            group = "General",
            sortOrder = 1,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-UPDATE-007: empty id returns 400")]
    public async Task UpdateIcon_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateIconEndpoint.Route.WithId(Guid.Empty), new
        {
            value = "🦊",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
