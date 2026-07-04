using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Icons;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Icons;

[EndpointFact(
    Feature = "Icons",
    Name = "Get Icons",
    Route = "GET /api/icons",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve the available icons so that I can pick one when creating or editing a wallet or category.",
    Description = new[]
    {
        "Client calls the endpoint with an optional `group` query parameter (`Wallet`, `Category`, or `General`).",
        "If `group` is provided, returns icons of that group plus all `General` icons. If `group` is omitted or unrecognised, returns all icons.",
        "Results are ordered by `SortOrder` ascending within each group.",
        "Each icon includes `id`, `value`, `name`, `group`, `sortOrder`, `isSystem`.",
    })]
public class GetIconsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ICONS-GET-001: GetIcons_WithoutToken_ReturnsUnauthorized")]
    public async Task GetIcons_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetIconsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-GET-002: GetIcons_WithToken_ReturnsAllSeededIcons")]
    public async Task GetIcons_WithToken_ReturnsAllSeededIcons()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetIconsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<IconResponse>>();
        body.ShouldNotBeNull();
        body!.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "ICONS-GET-003: GetIcons_WithGroupFilter_ReturnsGroupAndGeneralIcons")]
    public async Task GetIcons_WithGroupFilter_ReturnsGroupAndGeneralIcons()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync($"{GetIconsEndpoint.Route}?group=Wallet");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<IconResponse>>();
        body.ShouldNotBeNull();
        body!.ShouldNotBeEmpty();
        body.ShouldAllBe(i => i.Group == "Wallet" || i.Group == "General");
        body.ShouldContain(i => i.Group == "Wallet");
        body.ShouldContain(i => i.Group == "General");
    }

    [Fact(DisplayName = "ICONS-GET-004: GetIcons_WithInvalidGroup_ReturnsAllIcons")]
    public async Task GetIcons_WithInvalidGroup_ReturnsAllIcons()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var allResponse = await client.GetAsync(GetIconsEndpoint.Route);
        var allIcons = await allResponse.Content.ReadFromJsonAsync<List<IconResponse>>();

        // Act
        var response = await client.GetAsync($"{GetIconsEndpoint.Route}?group=NotAGroup");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<IconResponse>>();
        body!.Count.ShouldBe(allIcons!.Count);
    }

    [Fact(DisplayName = "ICONS-GET-005: GetIcons_ResultsOrderedBySortOrder")]
    public async Task GetIcons_ResultsOrderedBySortOrder()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync($"{GetIconsEndpoint.Route}?group=Wallet");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<IconResponse>>();
        var walletIcons = body!.Where(i => i.Group == "Wallet").ToList();
        walletIcons.Select(i => i.SortOrder).ShouldBeInOrder();
    }
}
