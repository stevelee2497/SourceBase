using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Auth;
using SourceBase.Api.Features.TodoLists;
using SourceBase.Api.Features.Todos;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class CreateTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "TODOS-CREATE-001: CreateTodo_WithoutToken_ReturnsUnauthorized")]
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "TODOS-CREATE-002: CreateTodo_WithValidData_ReturnsOk")]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "TODOS-CREATE-003: CreateTodo_WithValidData_SetsCreatedByToAuthenticatedUserName")]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
        body!.Id.Should().NotBeEmpty();

        var todo = await factory.WithDbContextAsync(db => db.TodoItems.SingleAsync(x => x.Id == body.Id));
        todo.CreatedBy.Should().Be(userInfo!.UserName);
        todo.UserId.Should().Be(userInfo.Id);
    }

    [Fact(DisplayName = "TODOS-CREATE-004: CreateTodo_WithMissingTitle_ReturnsBadRequest")]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TODOS-CREATE-005: CreateTodo_WithMissingDate_ReturnsBadRequest")]
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TODOS-CREATE-006: CreateTodo_WithValidTodoListId_ReturnsOk")]
    public async Task CreateTodo_WithValidTodoListId_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var listResponse = await client.PostAsJsonAsync("todo-lists", new { name = $"List_{Guid.NewGuid():N}" });
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
        var todo = await factory.WithDbContextAsync(db => db.TodoItems.SingleAsync(x => x.Id == body!.Id));
        todo!.TodoListId.Should().Be(list.Id);
    }

    [Fact(DisplayName = "TODOS-CREATE-007: CreateTodo_WithInvalidTodoListId_ReturnsNotFound")]
    public async Task CreateTodo_WithInvalidTodoListId_ReturnsNotFound()
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
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
