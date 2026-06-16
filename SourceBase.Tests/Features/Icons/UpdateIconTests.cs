using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Icons;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Icons;

public class UpdateIconTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ICONS-UPDATE-001: UpdateIcon_WithoutToken_ReturnsUnauthorized")]
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-UPDATE-002: UpdateIcon_WithValidRequest_ReturnsOk")]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateIconResponse>();
        body!.Id.Should().Be(created.Id);
    }

    [Fact(DisplayName = "ICONS-UPDATE-003: UpdateIcon_PersistsChanges")]
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
        updated.Should().NotBeNull();
        updated!.Value.Should().Be("🐺");
        updated.Name.Should().Be("Wolf_Updated");
        updated.Group.Should().Be("Category");
        updated.SortOrder.Should().Be(99);
    }

    [Fact(DisplayName = "ICONS-UPDATE-004: UpdateIcon_WithUnknownId_ReturnsNotFound")]
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
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "ICONS-UPDATE-005: UpdateIcon_OnSystemIcon_ReturnsForbidden")]
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
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "ICONS-UPDATE-006: UpdateIcon_WithEmptyValue_ReturnsBadRequest")]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
