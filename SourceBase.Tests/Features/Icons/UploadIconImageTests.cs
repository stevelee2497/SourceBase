using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Application.Features.Icons;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Icons;

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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ICONS-UPLOAD-002: UploadIconImage_WithEmptyFileName_ReturnsBadRequest")]
    public async Task UploadIconImage_WithEmptyFileName_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(UploadIconImageEndpoint.Route, new { fileName = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-UPLOAD-003: UploadIconImage_WithUnsupportedExtension_ReturnsBadRequest")]
    public async Task UploadIconImage_WithUnsupportedExtension_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(UploadIconImageEndpoint.Route, new { fileName = "icon.bmp" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "ICONS-UPLOAD-004: UploadIconImage_WithNoExtension_ReturnsBadRequest")]
    public async Task UploadIconImage_WithNoExtension_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(UploadIconImageEndpoint.Route, new { fileName = "iconfile" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
