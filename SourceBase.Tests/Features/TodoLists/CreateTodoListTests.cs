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
    Name = "Create Todo List",
    Route = "POST /api/todo-lists",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to create a named todo list, so that I can organise my tasks into logical groups.",
    Description = new[]
    {
        "Client sends `name` (required, max 200 characters).",
        "The todo list is created and associated with the authenticated user's ID.",
        "Returns the new list's `Id`.",
    })]
public class CreateTodoListTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOLISTS-CREATE-001: CreateTodoList_WithoutToken_ReturnsUnauthorized")]
    public async Task CreateTodoList_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "My List" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOLISTS-CREATE-002: CreateTodoList_WithValidData_ReturnsOk")]
    public async Task CreateTodoList_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Work Tasks" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoListResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "TODOLISTS-CREATE-003: CreateTodoList_WithMissingName_ReturnsBadRequest")]
    public async Task CreateTodoList_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TODOLISTS-CREATE-004: CreateTodoList_BelongsToAuthenticatedUser")]
    public async Task CreateTodoList_BelongsToAuthenticatedUser()
    {
        // Arrange
        var email = $"list_owner_{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Personal" });
        var body = await response.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var listsResponse = await client.GetAsync($"{GetTodoListsEndpoint.Route}?limit=100");
        var lists = await listsResponse.Content.ReadFromJsonAsync<PagingResponse<TodoListResponse>>();
        var list = lists!.Items.Single(x => x.Id == body!.Id);
        list.CreatedBy.ShouldNotBeNullOrEmpty();
    }
}
