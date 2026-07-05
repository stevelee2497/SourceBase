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
    Name = "Delete Machine",
    Route = "DELETE /api/machines/{id}",
    Auth = "Required",
    UseCase = "As a user, I want to delete a machine I no longer use.",
    Description = new[]
    {
        "Deletes the machine and all associated records.",
        "Returns 200 on success, 404 if machine not found or owned by another user.",
    })]
public class DeleteMachineTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "MACHINES-DELETE-001: missing token returns 401")]
    public async Task DeleteMachine_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.DeleteAsync(DeleteMachineEndpoint.Route.WithId(Guid.NewGuid()));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MACHINES-DELETE-002: delete machine returns 200")]
    public async Task DeleteMachine_WithValidId_ReturnsOk()
    {
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "DeleteTestMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await client.DeleteAsync(DeleteMachineEndpoint.Route.WithId(created!.Id));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "MACHINES-DELETE-003: non-existent machine returns 404")]
    public async Task DeleteMachine_WithNonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.DeleteAsync(DeleteMachineEndpoint.Route.WithId(Guid.NewGuid()));
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "MACHINES-DELETE-004: other user's machine returns 404")]
    public async Task DeleteMachine_WithOtherUsersMachine_ReturnsNotFound()
    {
        var user1 = await factory.CreateAuthorizedClient();
        var user2 = await factory.CreateAuthorizedClient($"{Guid.NewGuid():N}@test.com", "Test@1234!");
        var createResponse = await user1.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "User1Machine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await user2.DeleteAsync(DeleteMachineEndpoint.Route.WithId(created!.Id));
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
