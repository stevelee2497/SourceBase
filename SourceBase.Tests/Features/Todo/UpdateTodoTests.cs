using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Todos;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class UpdateTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOS-UPDATE-001: UpdateTodo_WithoutToken_ReturnsUnauthorized")]
    public async Task UpdateTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            date = "2025-08-01",
            title = "Updated",
            status = "Completed",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOS-UPDATE-002: UpdateTodo_WithValidData_ReturnsOk")]
    public async Task UpdateTodo_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-08-01",
            title = "To Be Updated",
            status = "Open",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoEndpoint.Route.WithId(createBody!.Id), new
        {
            date = "2025-08-01",
            title = "Updated Title",
            status = "Completed",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateTodoResponse>();
        body!.Id.ShouldBe(createBody.Id);

        var getResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(createBody.Id));
        var updated = await getResponse.Content.ReadFromJsonAsync<GetTodoResponse>();
        updated!.Title.ShouldBe("Updated Title");
        updated.Status.ShouldBe(TodoItemStatus.Completed);
    }

    [Fact(DisplayName = "TODOS-UPDATE-003: UpdateTodo_WithEmptyTitle_ReturnsBadRequest")]
    public async Task UpdateTodo_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-08-01",
            title = "To Be Updated",
            status = "Open",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoEndpoint.Route.WithId(createBody!.Id), new
        {
            title = "",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TODOS-UPDATE-004: UpdateTodo_WithNonExistentId_ReturnsNotFound")]
    public async Task UpdateTodo_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            date = "2025-08-01",
            title = "Updated",
            status = "Completed",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TODOS-UPDATE-005: UpdateTodo_WithOtherUsersTodo_ReturnsNotFound")]
    public async Task UpdateTodo_WithOtherUsersTodo_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"todo_owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var strangerClient = await factory.CreateAuthorizedClient($"todo_stranger_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-08-01",
            title = "Private Update",
            status = "Open",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await strangerClient.PatchAsJsonAsync(UpdateTodoEndpoint.Route.WithId(createBody!.Id), new
        {
            date = "2025-08-01",
            title = "Hacked",
            status = "Completed",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TODOS-UPDATE-006: UpdateTodo_WithEmptyId_ReturnsBadRequest")]
    public async Task UpdateTodo_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateTodoEndpoint.Route.WithId(Guid.Empty), new
        {
            title = "Test",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
