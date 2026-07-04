using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Data;
using SourceBase.Application.Features.Roles;
using SourceBase.Application.Features.Todos;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Data;

[EndpointFact(
    Feature = "Data",
    Name = "Get Audits",
    Route = "GET /api/data/audits",
    Auth = "Admin only",
    UseCase = "As an admin, I want to view the audit history of all data changes in the system, so that I can trace who changed what and when for compliance and debugging.",
    Description = new[]
    {
        "Admin sends optional paging parameters (`page`, `limit`, `order`).",
        "Results are sorted by `ActionOn` (most recent first by default).",
        "Each entry includes `author`, `action`, `entityType`, `entityId`, and JSON snapshots of the `current`, `original`, and `changes` state.",
        "Audit records are written automatically by `ApplicationDbContextAuditInterceptor` on every save — this endpoint only reads them.",
    })]
public class GetAuditsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "DATA-AUDITS-001: missing token return 401")]
    public async Task GetAudits_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetAuditsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DATA-AUDITS-002: non-admin user return 403")]
    public async Task GetAudits_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await client.GetAsync(GetAuditsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "DATA-AUDITS-003: admin user returns audit history")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<AuditHistoryResponse>>();
        body.ShouldNotBeNull();
        body!.Items.ShouldContain(x =>
            x.EntityId == createTodoBody!.Id.ToString() &&
            x.Author == actorInfo!.UserName &&
            x.Action == "Added" &&
            x.EntityType.EndsWith("TodoItemEntity", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "DATA-AUDITS-004: recent changes returns most recent entries first")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<AuditHistoryResponse>>();
        body.ShouldNotBeNull();
        body!.Items.Select(x => x.ActionOn).ShouldBeInOrder(SortDirection.Descending);
    }
}
