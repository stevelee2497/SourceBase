using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Icons;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Icons;

public class DeleteIconTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ICONS-DELETE-001: DeleteIcon_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteIcon_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-DELETE-002: DeleteIcon_WithValidId_ReturnsSuccess")]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteIconResponse>();
        body!.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "ICONS-DELETE-003: DeleteIcon_RemovedFromGetIcons")]
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
        icons.Should().NotContain(i => i.Id == created.Id);
    }

    [Fact(DisplayName = "ICONS-DELETE-004: DeleteIcon_WithUnknownId_ReturnsBadRequest")]
    public async Task DeleteIcon_WithUnknownId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-DELETE-005: DeleteIcon_OnSystemIcon_ReturnsBadRequest")]
    public async Task DeleteIcon_OnSystemIcon_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var iconsResponse = await client.GetAsync(GetIconsEndpoint.Route);
        var icons = await iconsResponse.Content.ReadFromJsonAsync<List<IconResponse>>();
        var systemIcon = icons!.First(i => i.IsSystem);

        // Act
        var response = await client.DeleteAsync(DeleteIconEndpoint.Route.WithId(systemIcon.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
