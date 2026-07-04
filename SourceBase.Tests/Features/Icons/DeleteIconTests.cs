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
    Name = "Delete Icon",
    Route = "DELETE /api/icons/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to delete a custom icon I no longer need.",
    Description = new[]
    {
        "Client provides the icon `id` (route).",
        "If the icon doesn't exist → `404 Not Found`.",
        "If the icon is a system icon → `403 Forbidden`.",
        "The icon is removed.",
    })]
public class DeleteIconTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ICONS-DELETE-001: without token returns 401")]
    public async Task DeleteIcon_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-DELETE-002: valid id returns 200")]
    public async Task DeleteIcon_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var createResponse = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🗑️",
            name = $"ToDelete_{Guid.NewGuid():N}",
            group = "General",
            sortOrder = 1,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateIconResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(created!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteIconResponse>();
        body!.Success.ShouldBeTrue();
    }

    [Fact(DisplayName = "ICONS-DELETE-003: deleted icon removed from list")]
    public async Task DeleteIcon_RemovedFromGetIcons()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var uniqueName = $"Delete_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🗑️",
            name = uniqueName,
            group = "General",
            sortOrder = 1,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateIconResponse>();

        // Act
        await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(created!.Id));

        // Assert
        var allResponse = await client.GetAsync(GetIconsEndpoint.Route);
        var icons = await allResponse.Content.ReadFromJsonAsync<List<IconResponse>>();
        icons!.ShouldNotContain(i => i.Id == created.Id);
    }

    [Fact(DisplayName = "ICONS-DELETE-004: unknown id returns 404")]
    public async Task DeleteIcon_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "ICONS-DELETE-005: system icon returns 403")]
    public async Task DeleteIcon_OnSystemIcon_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var iconsResponse = await client.GetAsync(GetIconsEndpoint.Route);
        var icons = await iconsResponse.Content.ReadFromJsonAsync<List<IconResponse>>();
        var systemIcon = icons!.First(i => i.IsSystem);

        // Act
        var response = await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(systemIcon.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
