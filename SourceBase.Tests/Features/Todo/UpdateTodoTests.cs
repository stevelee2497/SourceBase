using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Todos;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class UpdateTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "TODOS-UPDATE-001")]
    [Fact]
    public async Task UpdateTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTodoEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            date = "2025-08-01",
            title = "Updated",
            status = "Completed",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "TODOS-UPDATE-002")]
    [Fact]
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
        var response = await client.PutAsJsonAsync(UpdateTodoEndpoint.Route.WithId(createBody!.Id), new
        {
            date = "2025-08-01",
            title = "Updated Title",
            status = "Completed",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateTodoResponse>();
        body!.Id.Should().Be(createBody.Id);

        var getResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(createBody.Id));
        var updated = await getResponse.Content.ReadFromJsonAsync<GetTodoResponse>();
        updated!.Title.Should().Be("Updated Title");
        updated.Status.Should().Be(TodoItemStatus.Completed);
    }
    [Trait("TestCaseId", "TODOS-UPDATE-003")]
    [Fact]
    public async Task UpdateTodo_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTodoEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            date = "2025-08-01",
            status = "Completed",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Trait("TestCaseId", "TODOS-UPDATE-004")]
    [Fact]
    public async Task UpdateTodo_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateTodoEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            date = "2025-08-01",
            title = "Updated",
            status = "Completed",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Trait("TestCaseId", "TODOS-UPDATE-005")]
    [Fact]
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
        var response = await strangerClient.PutAsJsonAsync(UpdateTodoEndpoint.Route.WithId(createBody!.Id), new
        {
            date = "2025-08-01",
            title = "Hacked",
            status = "Completed",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
