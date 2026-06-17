using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.TodoLists;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TodoLists;

public class DeleteTodoListTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOLISTS-DELETE-001: DeleteTodoList_WithoutToken_ReturnsUnauthorized")]
    public async Task DeleteTodoList_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(DeleteTodoListEndpoint.Route.WithId(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOLISTS-DELETE-002: DeleteTodoList_WithValidId_ReturnsOk")]
    public async Task DeleteTodoList_WithValidId_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "To Delete" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await client.DeleteAsync(DeleteTodoListEndpoint.Route.WithId(created!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteTodoListResponse>();
        body!.Success.ShouldBeTrue();
    }

    [Fact(DisplayName = "TODOLISTS-DELETE-003: DeleteTodoList_OwnedByAnotherUser_ReturnsNotFound")]
    public async Task DeleteTodoList_OwnedByAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"del_owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"del_other_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Owner's" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await otherClient.DeleteAsync(DeleteTodoListEndpoint.Route.WithId(created!.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
