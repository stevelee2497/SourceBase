using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.TodoLists;
using SourceBase.Application.Features.Todos;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

[EndpointFact(
    Feature = "Todos",
    Name = "Create Todo",
    Route = "POST /api/todos",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to create a todo item with a title, date, and status, so that I can track individual tasks.",
    Description = new[]
    {
        "Client sends `title` (required), `date` (required), `status`, and an optional `todoListId`.",
        "If `todoListId` is provided but doesn't exist or doesn't belong to the current user → `404 Not Found`.",
        "The todo item is created and associated with the authenticated user and optionally a todo list.",
        "Returns the new item's `Id`.",
    })]
public class CreateTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOS-CREATE-001: missing token returns 401")]
    public async Task CreateTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-01-01",
            title = "Test",
            status = "Open",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOS-CREATE-002: valid data returns 200")]
    public async Task CreateTodo_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-06-01",
            title = "Integration Test Todo",
            status = "Open",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "TODOS-CREATE-003: valid data sets created by to authenticated user name")]
    public async Task CreateTodo_WithValidData_SetsCreatedByToAuthenticatedUserName()
    {
        // Arrange
        var email = $"{Guid.NewGuid():N}@test.com";
        var client = await factory.CreateAuthorizedClient(email, "Test@1234!");

        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        var title = $"Audit_{Guid.NewGuid():N}";

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-10-01",
            title,
            status = "Open",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);

        var todoResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(body.Id));
        var todo = await todoResponse.Content.ReadFromJsonAsync<GetTodoResponse>();
        todo!.CreatedBy.ShouldBe(userInfo!.UserName);
        todo.UserId.ShouldBe(userInfo.Id);
    }

    [Fact(DisplayName = "TODOS-CREATE-004: missing title returns 400")]
    public async Task CreateTodo_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-06-01",
            status = "Open",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TODOS-CREATE-005: missing date returns 400")]
    public async Task CreateTodo_WithMissingDate_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            title = "Missing Date",
            status = "Open",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TODOS-CREATE-006: valid todo list id returns 200")]
    public async Task CreateTodo_WithValidTodoListId_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var listResponse = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = $"List_{Guid.NewGuid():N}" });
        var list = await listResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-06-01",
            title = "Todo in list",
            status = "Open",
            todoListId = list!.Id,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
        var todoResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(body!.Id));
        var todo = await todoResponse.Content.ReadFromJsonAsync<GetTodoResponse>();
        todo!.TodoListId.ShouldBe(list.Id);
    }

    [Fact(DisplayName = "TODOS-CREATE-007: invalid todo list id returns 400")]
    public async Task CreateTodo_WithInvalidTodoListId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-06-01",
            title = "Todo with bad list",
            status = "Open",
            todoListId = Guid.NewGuid(),
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
