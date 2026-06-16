using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Todos;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class GetTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOS-GET-001: GetTodo_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOS-GET-002: GetTodo_AfterCreate_ReturnsCorrectData")]
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

    [Fact(DisplayName = "TODOS-GET-003: GetTodo_NonExistentId_ReturnsBadRequest")]
    public async Task GetTodo_NonExistentId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TODOS-GET-004: GetTodo_WithOtherUsersTodo_ReturnsBadRequest")]
    public async Task GetTodo_WithOtherUsersTodo_ReturnsBadRequest()
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
