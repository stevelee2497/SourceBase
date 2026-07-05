using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Machines;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Machines;

[EndpointFact(
    Feature = "Machines",
    Name = "Restart Machine",
    Route = "POST /api/machines/{id}/restart",
    Auth = "Required",
    UseCase = "As a user, I want to send a restart command to a remote machine.",
    Description = new[]
    {
        "Sends restart command via SignalR to the user's machine group.",
        "Returns 200 with confirmation message, 404 if machine not found or owned by another user.",
    })]
public class RestartMachineTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "MACHINES-RESTART-001: valid machine returns 200")]
    public async Task RestartMachine_WithValidId_ReturnsOk()
    {
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "RestartTestMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await client.PostAsJsonAsync(RestartMachineEndpoint.Route.WithId(created!.Id), new { });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RestartMachineResponse>();
        body!.Message.ShouldContain("sent");
    }

    [Fact(DisplayName = "MACHINES-RESTART-002: non-existent machine returns 404")]
    public async Task RestartMachine_WithNonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync(RestartMachineEndpoint.Route.WithId(Guid.NewGuid()), new { });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
