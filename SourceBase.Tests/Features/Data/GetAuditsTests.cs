using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Auth;
using SourceBase.Api.Features.Data;
using SourceBase.Api.Features.Roles;
using SourceBase.Api.Features.Todos;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

public class GetAuditsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "DATA-AUDITS-001")]
    [Fact]
    public async Task GetAudits_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetAuditsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "DATA-AUDITS-002")]
    [Fact]
    public async Task GetAudits_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetAuditsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    [Trait("TestCaseId", "DATA-AUDITS-003")]
    [Fact]
    public async Task GetAudits_WithAdminUser_ReturnsAuditHistory()
    {
        // Arrange
        var actorEmail = $"{Guid.NewGuid():N}@test.com";
        var actorClient = await factory.CreateAuthorizedClient(actorEmail, "Test@1234!");

        var actorInfoResponse = await actorClient.GetAsync(GetUserInfoEndpoint.Route);
        var actorInfo = await actorInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        var createTodoResponse = await actorClient.PostAsJsonAsync(CreateTodoEndpoint.Route, new
        {
            date = "2025-01-01",
            title = $"Audit_{Guid.NewGuid():N}",
            status = "Open",
        });
        var createTodoBody = await createTodoResponse.Content.ReadFromJsonAsync<CreateTodoResponse>();
        var adminClient = await factory.CreateAuthorizedClient();

        // Act
        var response = await adminClient.GetAsync(GetAuditsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<AuditHistoryResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(x =>
            x.EntityId == createTodoBody!.Id.ToString() &&
            x.Author == actorInfo!.UserName &&
            x.Action == "Added" &&
            x.EntityType.EndsWith("TodoItemEntity", StringComparison.Ordinal));
    }
    [Trait("TestCaseId", "DATA-AUDITS-004")]
    [Fact]
    public async Task GetAudits_WithRecentChanges_ReturnsMostRecentEntriesFirst()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();

        await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "First role",
        });
        await Task.Delay(20);
        await adminClient.PostAsJsonAsync(CreateRoleEndpoint.Route, new
        {
            name = $"Role_{Guid.NewGuid():N}",
            description = "Second role",
        });

        // Act
        var response = await adminClient.GetAsync(GetAuditsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<AuditHistoryResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().BeInDescendingOrder(x => x.ActionOn);
    }
}
