using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Todos;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class DeleteTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOS-DELETE-001: DeleteTodo_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOS-DELETE-002: DeleteTodo_ExistingItem_ReturnsOk")]
    public async Task DeleteTodo_ExistingItem_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-09-01",
            title = "To Be Deleted",
            status = "Open",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTodoEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteTodoResponse>();
        body!.Success.ShouldBeTrue();

        var getResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(createBody.Id));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TODOS-DELETE-003: DeleteTodo_WithNonExistentId_ReturnsNotFound")]
    public async Task DeleteTodo_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(DeleteTodoEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "TODOS-DELETE-004: DeleteTodo_WithOtherUsersTodo_ReturnsNotFound")]
    public async Task DeleteTodo_WithOtherUsersTodo_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"delete_owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var strangerClient = await factory.CreateAuthorizedClient($"delete_stranger_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-09-01",
            title = "Private Delete",
            status = "Open",
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await strangerClient.DeleteAsync(DeleteTodoEndpoint.Route.WithId(createBody!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
