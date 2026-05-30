using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.TodoLists;
using SourceBase.Api.Features.Todos;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Todo;

public class GetTodosTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "TODOS-GET-ALL-001")]
    [Fact]
    public async Task GetTodos_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetTodosEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "TODOS-GET-ALL-002")]
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
    [Trait("TestCaseId", "TODOS-GET-ALL-003")]
    [Fact]
    public async Task GetTodos_WithMultipleUsers_ReturnsOnlyCurrentUsersItems()
    {
        // Arrange
        var firstClient = await factory.CreateAuthorizedClient($"first_{Guid.NewGuid():N}@test.com", "Test@1234!");
        var secondClient = await factory.CreateAuthorizedClient($"second_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var ownTodoResponse = await firstClient.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-01-01",
            title = "Own Todo",
            status = "Open",
        });
        var ownTodoBody = await ownTodoResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();
        await secondClient.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-01-01",
            title = "Other Todo",
            status = "Open",
        });

        // Act
        var response = await firstClient.GetAsync(GetTodosEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        todos!.Items.Should().ContainSingle(x => x.Id == ownTodoBody!.Id);
        todos.Items.Should().NotContain(x => x.Title == "Other Todo");
    }
    [Trait("TestCaseId", "TODOS-GET-ALL-004")]
    [Fact]
    public async Task GetTodos_WithStatusAndDateFilters_ReturnsMatchingItems()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"filters_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var matchingTodoResponse = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-02-01",
            title = "Matching",
            status = "Completed",
        });
        var matchingTodoBody = await matchingTodoResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();
        await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-02-01",
            title = "Wrong Status",
            status = "Open",
        });
        await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-03-01",
            title = "Wrong Date",
            status = "Completed",
        });

        // Act
        var response = await client.GetAsync($"{GetTodosEndpoint.Route}?status=Completed&date=2025-02-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        todos!.Items.Should().ContainSingle(x => x.Id == matchingTodoBody!.Id);
    }
    [Trait("TestCaseId", "TODOS-GET-ALL-005")]
    [Fact]
    public async Task GetTodos_WithPagingAndOrdering_ReturnsRequestedPage()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"paging_{Guid.NewGuid():N}@test.com", "Test@1234!");

        await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-04-01",
            title = "Alpha",
            status = "Open",
        });
        var expectedTodoResponse = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-04-01",
            title = "Zulu",
            status = "Open",
        });
        var expectedTodoBody = await expectedTodoResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        // Act
        var response = await client.GetAsync($"{GetTodosEndpoint.Route}?orderBy=Title&order=Desc&page=1&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        todos!.Page.Should().Be(1);
        todos.Limit.Should().Be(1);
        todos.Items.Should().ContainSingle();
        todos.Items.Single().Id.Should().Be(expectedTodoBody!.Id);
    }
    [Trait("TestCaseId", "TODOS-GET-ALL-006")]
    [Fact]
    public async Task GetTodos_FilteredByTodoListId_ReturnsMatchingItems()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"list_filter_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var listResponse = await client.PostAsJsonAsync("todo-lists", new { name = $"Filter_List_{Guid.NewGuid():N}" });
        var list = await listResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        var inListResponse = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-05-01",
            title = "In List Todo",
            status = "Open",
            todoListId = list!.Id,
        });
        var inListBody = await inListResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();

        await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-05-01",
            title = "No List Todo",
            status = "Open",
        });

        // Act
        var response = await client.GetAsync($"{GetTodosEndpoint.Route}?todoListId={list.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<PagingResponse<GetTodoResponse>>();
        todos!.Items.Should().ContainSingle(x => x.Id == inListBody!.Id);
        todos.Items.Should().NotContain(x => x.Title == "No List Todo");
    }
}
