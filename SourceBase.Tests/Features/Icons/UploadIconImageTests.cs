using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Icons;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Icons;

[EndpointFact(
    Feature = "Icons",
    Name = "Upload Icon Image",
    Route = "POST /api/icons/upload-image",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to upload an image file as an icon so that I can use a custom image in the icon picker.",
    Description = new[]
    {
        "Client sends `fileName` (required) with a supported extension: `jpg`, `jpeg`, `png`, `gif`, `webp`, or `svg`.",
        "The API generates a presigned upload URL pointing to Cloudflare R2 storage and returns the final public `iconUrl` and `contentType`.",
        "Client uploads the file directly to the presigned URL (PUT request), then uses the returned `iconUrl` as the icon `value` when creating an icon.",
    })]
public class UploadIconImageTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "ICONS-UPLOAD-001: UploadIconImage_WithoutToken_ReturnsUnauthorized")]
    public async Task UploadIconImage_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(UploadIconImageEndpoint.Route, new { fileName = "icon.png" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-UPLOAD-002: UploadIconImage_WithEmptyFileName_ReturnsBadRequest")]
    public async Task UploadIconImage_WithEmptyFileName_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(UploadIconImageEndpoint.Route, new { fileName = "" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-UPLOAD-003: UploadIconImage_WithUnsupportedExtension_ReturnsBadRequest")]
    public async Task UploadIconImage_WithUnsupportedExtension_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(UploadIconImageEndpoint.Route, new { fileName = "icon.bmp" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-UPLOAD-004: UploadIconImage_WithNoExtension_ReturnsBadRequest")]
    public async Task UploadIconImage_WithNoExtension_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(UploadIconImageEndpoint.Route, new { fileName = "iconfile" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
