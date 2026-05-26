using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Todos;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class GetTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task GetTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTodo_AfterCreate_ReturnsCorrectData()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-07-15",
            title = "Fetch Me",
            status = "Open",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTodoResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("Fetch Me");
        body.Status.Should().Be(TodoItemStatus.Open);
        body.Date.Should().Be(new DateOnly(2025, 7, 15));
    }

    [Fact]
    public async Task GetTodo_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTodo_WithOtherUsersTodo_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var strangerClient = await factory.CreateAuthorizedClient($"stranger_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-07-15",
            title = "Private Todo",
            status = "Open",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await strangerClient.GetAsync(GetTodoEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
