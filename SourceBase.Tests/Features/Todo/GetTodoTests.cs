using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Todos;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

[EndpointFact(
    Feature = "Todos",
    Name = "Get Todo",
    Route = "GET /api/todos/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to retrieve the details of a specific todo item, so that I can view its full information.",
    Description = new[]
    {
        "Client provides the todo `id` (route).",
        "If the item doesn't exist or belongs to a different user → `404 Not Found`.",
        "Returns the full todo item details.",
    })]
public class GetTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOS-GET-001: missing token returns 401")]
    public async Task GetTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOS-GET-002: returns correct data after creation")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTodoResponse>();
        body.ShouldNotBeNull();
        body!.Title.ShouldBe("Fetch Me");
        body.Status.ShouldBe(TodoItemStatus.Open);
        body.Date.ShouldBe(new DateOnly(2025, 7, 15));
    }

    [Fact(DisplayName = "TODOS-GET-003: nonexistent id returns 404")]
    public async Task GetTodo_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TODOS-GET-004: other user's todo returns 404")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
