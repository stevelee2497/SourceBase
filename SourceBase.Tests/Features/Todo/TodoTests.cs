using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Todos;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class TodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    // ── Unauthorized access ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTodos_WithoutToken_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync(GetTodosEndpoint.Route);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTodo_WithoutToken_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-01-01",
            title = "Test",
            status = "Open",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── CRUD happy paths ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTodos_Authenticated_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetTodosEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Fact]
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

    [Fact]
    public async Task CreateTodo_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-06-01",
            status = "Open",
            // title intentionally omitted
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTodo_WithMissingDate_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            title = "Missing Date",
            status = "Open",
            // date intentionally omitted
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodo_AfterCreate_ReturnsCorrectData()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new { date = "2025-07-15", title = "Fetch Me", status = "Open" });
        var list = await client.GetAsync($"{GetTodosEndpoint.Route}?date=2025-07-15");
        var todos = await list.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        var id = todos!.Items.First(x => x.Title == "Fetch Me").Id;

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todo = await response.Content.ReadFromJsonAsync<GetTodoResponse>();
        todo.Should().NotBeNull();
        todo!.Title.Should().Be("Fetch Me");
        todo.Status.Should().Be(TodoItemStatus.Open);
    }

    [Fact]
    public async Task UpdateTodo_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new { date = "2025-08-01", title = "To Be Updated", status = "Open" });
        var list = await client.GetAsync($"{GetTodosEndpoint.Route}?date=2025-08-01");
        var todos = await list.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        var id = todos!.Items.First(x => x.Title == "To Be Updated").Id;

        // Act
        var response = await client.PutAsJsonAsync(UpdateTodoEndpoint.Route.WithId(id), new
        {
            date = "2025-08-01",
            title = "Updated Title",
            status = "Completed",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateTodoResponse>();
        body!.Id.Should().Be(id);
        var updated = await (await client.GetAsync(GetTodoEndpoint.Route.WithId(id))).Content.ReadFromJsonAsync<GetTodoResponse>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.Status.Should().Be(TodoItemStatus.Completed);
    }

    [Fact]
    public async Task DeleteTodo_ExistingItem_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new { date = "2025-09-01", title = "To Be Deleted", status = "Open" });
        var list = await client.GetAsync($"{GetTodosEndpoint.Route}?date=2025-09-01");
        var todos = await list.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        var id = todos!.Items.First(x => x.Title == "To Be Deleted").Id;

        // Act
        var response = await client.DeleteAsync(DeleteTodoEndpoint.Route.WithId(id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteTodoResponse>();
        body!.Success.Should().BeTrue();
        var getResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(id));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTodo_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var id = Guid.NewGuid();

        // Act
        var response = await client.GetAsync(GetTodoEndpoint.Route.WithId(id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
