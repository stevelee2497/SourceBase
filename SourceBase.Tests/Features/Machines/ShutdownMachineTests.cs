using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Machines;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Machines;

[EndpointFact(
    Feature = "Machines",
    Name = "Shutdown Machine",
    Route = "POST /api/machines/{id}/shutdown",
    Auth = "Required",
    UseCase = "As a user, I want to send a shutdown command to a remote machine.",
    Description = new[]
    {
        "Sends shutdown command via SignalR to the user's machine group.",
        "Returns 200 with confirmation message, 404 if machine not found or owned by another user.",
    })]
public class ShutdownMachineTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "MACHINES-SHUTDOWN-001: missing token returns 401")]
    public async Task ShutdownMachine_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"machines/{Guid.NewGuid()}/shutdown", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MACHINES-SHUTDOWN-002: valid machine returns 200")]
    public async Task ShutdownMachine_WithValidId_ReturnsOk()
    {
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "ShutdownTestMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await client.PostAsJsonAsync($"machines/{created!.Id}/shutdown", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ShutdownMachineResponse>();
        body!.Message.ShouldContain("sent");
    }

    [Fact(DisplayName = "MACHINES-SHUTDOWN-003: non-existent machine returns 404")]
    public async Task ShutdownMachine_WithNonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync($"machines/{Guid.NewGuid()}/shutdown", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "MACHINES-SHUTDOWN-004: other user's machine returns 404")]
    public async Task ShutdownMachine_WithOtherUsersMachine_ReturnsNotFound()
    {
        var user1 = await factory.CreateAuthorizedClient();
        var user2 = await factory.CreateAuthorizedClient($"{Guid.NewGuid():N}@test.com", "Test@1234!");
        var createResponse = await user1.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "User1ShutdownMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await user2.PostAsJsonAsync($"machines/{created!.Id}/shutdown", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
