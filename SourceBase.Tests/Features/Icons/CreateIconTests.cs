using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Icons;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Icons;

[EndpointFact(
    Feature = "Icons",
    Name = "Create Icon",
    Route = "POST /api/icons",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to add a custom icon so that wallets and categories can display it.",
    Description = new[]
    {
        "Client sends `value` (required, max 2000 characters — emoji, SVG markup, or image URL), `name` (required, max 100 characters), `group` (required: `Wallet`, `Category`, or `General`), and `sortOrder` (required).",
        "The icon is created with `IsSystem = false`.",
        "Returns the new icon's `Id`.",
    })]
public class CreateIconTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ICONS-CREATE-001: missing token returns 401")]
    public async Task CreateIcon_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "⭐",
            name = "Star",
            group = "General",
            sortOrder = 99,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-CREATE-002: valid request returns 200 and icon id")]
    public async Task CreateIcon_WithValidRequest_ReturnsId()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🦊",
            name = $"Fox_{Guid.NewGuid():N}",
            group = "General",
            sortOrder = 99,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateIconResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "ICONS-CREATE-003: empty value returns 400")]
    public async Task CreateIcon_WithEmptyValue_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "",
            name = "Test",
            group = "General",
            sortOrder = 1,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-CREATE-004: empty name returns 400")]
    public async Task CreateIcon_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🦊",
            name = "",
            group = "General",
            sortOrder = 1,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-CREATE-005: value exceeding max length returns 200")]
    public async Task CreateIcon_WithValueTooLong_OK()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = new string('x', 2001),
            name = "Test",
            group = "General",
            sortOrder = 1,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "ICONS-CREATE-006: name exceeding max length returns 400")]
    public async Task CreateIcon_WithNameTooLong_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🦊",
            name = new string('x', 101),
            group = "General",
            sortOrder = 1,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-CREATE-007: created icon appears in get icons")]
    public async Task CreateIcon_AppearsInGetIcons()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var uniqueName = $"Unique_{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(CreateIconEndpoint.Route, new
        {
            value = "🦋",
            name = uniqueName,
            group = "General",
            sortOrder = 999,
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync(GetIconsEndpoint.Route);

        // Assert
        var icons = await response.Content.ReadFromJsonAsync<List<IconResponse>>();
        icons!.ShouldContain(i => i.Name == uniqueName);
    }
}
