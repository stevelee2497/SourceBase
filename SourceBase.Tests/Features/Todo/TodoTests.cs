using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Todo;
using SourceBase.Tests.Infrastructure;

namespace SourceBase.Tests.Features.Todo;

[TestFixture]
public class TodoTests
{
    private WebAppFactory _factory = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WebAppFactory();
        await _factory.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown() => await _factory.DisposeAsync();

    // ── Unauthorized access ───────────────────────────────────────────────────

    [Test]
    public async Task GetTodos_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // new client without auth header

        // Act
        var response = await client.GetAsync("/api/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // new client without auth header

        // Act
        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            date = "2025-01-01",
            title = "Test",
            status = "Open",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── CRUD happy paths ──────────────────────────────────────────────────────

    [Test]
    public async Task GetTodos_Authenticated_ReturnsOk()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync("/api/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTodosResponse>(WebAppFactory.JsonOptions);
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Test]
    public async Task CreateTodo_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            date = "2025-06-01",
            title = "Integration Test Todo",
            status = "Open",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CreateTodo_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            date = "2025-06-01",
            status = "Open",
            // title intentionally omitted
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTodo_WithMissingDate_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            title = "Missing Date",
            status = "Open",
            // date intentionally omitted
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetTodo_AfterCreate_ReturnsCorrectData()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync("/api/todos", new { date = "2025-07-15", title = "Fetch Me", status = "Open" });
        var list = await client.GetAsync("/api/todos?date=2025-07-15");
        var todos = await list.Content.ReadFromJsonAsync<GetTodosResponse>(WebAppFactory.JsonOptions);
        var id = todos!.Items.First(x => x.Title == "Fetch Me").Id;

        // Act
        var response = await client.GetAsync($"/api/todos/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todo = await response.Content.ReadFromJsonAsync<GetTodoResponse>(WebAppFactory.JsonOptions);
        todo.Should().NotBeNull();
        todo!.Title.Should().Be("Fetch Me");
        todo.Status.Should().Be(TodoItemStatus.Open);
    }

    [Test]
    public async Task UpdateTodo_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync("/api/todos", new { date = "2025-08-01", title = "To Be Updated", status = "Open" });
        var list = await client.GetAsync("/api/todos?date=2025-08-01");
        var todos = await list.Content.ReadFromJsonAsync<GetTodosResponse>(WebAppFactory.JsonOptions);
        var id = todos!.Items.First(x => x.Title == "To Be Updated").Id;

        // Act
        var response = await client.PutAsJsonAsync($"/api/todos/{id}", new
        {
            date = "2025-08-01",
            title = "Updated Title",
            status = "Completed",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var updated = await (await client.GetAsync($"/api/todos/{id}")).Content.ReadFromJsonAsync<GetTodoResponse>(WebAppFactory.JsonOptions);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.Status.Should().Be(TodoItemStatus.Completed);
    }

    [Test]
    public async Task DeleteTodo_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync("/api/todos", new { date = "2025-09-01", title = "To Be Deleted", status = "Open" });
        var list = await client.GetAsync("/api/todos?date=2025-09-01");
        var todos = await list.Content.ReadFromJsonAsync<GetTodosResponse>(WebAppFactory.JsonOptions);
        var id = todos!.Items.First(x => x.Title == "To Be Deleted").Id;

        // Act
        var response = await client.DeleteAsync($"/api/todos/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await client.GetAsync($"/api/todos/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound); 
    }

    [Test]
    public async Task GetTodo_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await _factory.CreateAuthorizedClient();
        var id = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/todos/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); 
    }
}
