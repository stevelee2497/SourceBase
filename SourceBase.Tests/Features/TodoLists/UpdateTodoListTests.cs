using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.TodoLists;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.TodoLists;

public class UpdateTodoListTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "TODOLISTS-UPDATE-001")]
    [Fact]
    public async Task UpdateTodoList_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"todo-lists/{Guid.NewGuid()}", new { name = "Updated" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "TODOLISTS-UPDATE-002")]
    [Fact]
    public async Task UpdateTodoList_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Original" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await client.PutAsJsonAsync($"todo-lists/{created!.Id}", new { name = "Updated Name" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateTodoListResponse>();
        body!.Id.Should().Be(created.Id);
    }
    [Trait("TestCaseId", "TODOLISTS-UPDATE-003")]
    [Fact]
    public async Task UpdateTodoList_OwnedByAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var ownerClient = await factory.CreateAuthorizedClient($"owner_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var otherClient = await factory.CreateAuthorizedClient($"other_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var createResponse = await ownerClient.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = "Owner's List" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await otherClient.PutAsJsonAsync($"todo-lists/{created!.Id}", new { name = "Hijacked" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
