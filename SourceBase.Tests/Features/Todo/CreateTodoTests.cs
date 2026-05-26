using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Auth;
using SourceBase.Api.Features.Todos;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class CreateTodoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
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
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
