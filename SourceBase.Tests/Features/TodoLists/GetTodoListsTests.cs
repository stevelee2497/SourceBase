using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.TodoLists;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TodoLists;

public class GetTodoListsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOLISTS-GET-001: GetTodoLists_WithoutToken_ReturnsUnauthorized")]
    public async Task GetTodoLists_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTodoListsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOLISTS-GET-002: GetTodoLists_ReturnsOk")]
    public async Task GetTodoLists_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTodoListsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<TodoListResponse>>();
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
    }

    [Fact(DisplayName = "TODOLISTS-GET-003: GetTodoLists_ReturnsOnlyCurrentUserLists")]
    public async Task GetTodoLists_ReturnsOnlyCurrentUserLists()
    {
        // Arrange
        var emailA = $"user_a_{Guid.NewGuid():N}@test.com";
        var emailB = $"user_b_{Guid.NewGuid():N}@test.com";
        var clientA = await factory.CreateAuthorizedClient(emailA, "Test@1234!");
        var clientB = await factory.CreateAuthorizedClient(emailB, "Test@1234!");

        var listName = $"List_{Guid.NewGuid():N}";
        await clientA.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = listName });

        // Act
        var responseB = await clientB.GetAsync(GetTodoListsEndpoint.Route);
        var bodyB = await responseB.Content.ReadFromJsonAsync<PagingResponse<TodoListResponse>>();

        // Assert
        bodyB!.Items.ShouldNotContain(x => x.Name == listName);
    }
}
