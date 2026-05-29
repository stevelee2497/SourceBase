using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.TodoLists;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TodoLists;

public class CreateTodoListTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task CreateTodoList_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "My List" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTodoList_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Work Tasks" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoListResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTodoList_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTodoList_BelongsToAuthenticatedUser()
    {
        // Arrange
        var email = $"list_owner_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Personal" });
        var body = await response.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await factory.WithDbContextAsync(db => db.TodoLists.SingleAsync(x => x.Id == body!.Id));
        list.CreatedBy.Should().NotBeNullOrEmpty();
    }
}
