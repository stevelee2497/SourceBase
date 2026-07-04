using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.TodoLists;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TodoLists;

[EndpointFact(
    Feature = "Todos",
    Name = "Update Todo List",
    Route = "PATCH /api/todo-lists/{id}",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to partially update one of my todo lists, so that I can keep its name relevant to my tasks without sending all fields.",
    Description = new[]
    {
        "Client sends the list `id` (route) and any subset of: `name`. All fields are optional — only provided (non-null) fields are updated.",
        "If the list doesn't exist or belongs to a different user → `404 Not Found`.",
        "Returns the updated list's `Id`.",
    })]
public class UpdateTodoListTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOLISTS-UPDATE-001: missing token returns 401")]
    public async Task UpdateTodoList_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoListEndpoint.Route.WithId(Guid.NewGuid()), new { name = "Updated" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOLISTS-UPDATE-002: valid data returns 200")]
    public async Task UpdateTodoList_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Original" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoListEndpoint.Route.WithId(created!.Id), new { name = "Updated Name" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateTodoListResponse>();
        body!.Id.ShouldBe(created.Id);
    }

    [Fact(DisplayName = "TODOLISTS-UPDATE-003: list owned by another user returns 404")]
    public async Task UpdateTodoList_OwnedByAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"other_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Owner's List" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await otherClient.PatchAsJsonAsync(UpdateTodoListEndpoint.Route.WithId(created!.Id), new { name = "Hijacked" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TODOLISTS-UPDATE-004: empty id returns 400")]
    public async Task UpdateTodoList_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoListEndpoint.Route.WithId(Guid.Empty), new { name = "Test" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
