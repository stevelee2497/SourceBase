using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Machines;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Machines;

public class MachineTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "MACHINES-CREATE-001: missing token returns 401")]
    public async Task CreateMachine_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "test-machine" });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MACHINES-CREATE-002: valid name returns 200")]
    public async Task CreateMachine_WithValidName_ReturnsOk()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "TestMachine" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateMachineResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "MACHINES-CREATE-003: empty name returns 400")]
    public async Task CreateMachine_WithEmptyName_ReturnsBadRequest()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "" });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "MACHINES-GET-001: missing token returns 401")]
    public async Task GetMachines_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(GetMachinesEndpoint.Route);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MACHINES-GET-002: returns user's machines")]
    public async Task GetMachines_WithToken_ReturnsMachines()
    {
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "MyMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await client.GetAsync(GetMachinesEndpoint.Route);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        body!.Items.Count.ShouldBeGreaterThan(0);
        var machine = body.Items.FirstOrDefault(m => m.Id == created!.Id);
        machine.ShouldNotBeNull();
        machine.Name.ShouldBe("MyMachine");
    }

    [Fact(DisplayName = "MACHINES-UPDATE-001: missing token returns 401")]
    public async Task UpdateMachine_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.PatchAsJsonAsync($"machines/{Guid.NewGuid()}", new { alias = "NewAlias" });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MACHINES-UPDATE-002: update alias returns 200")]
    public async Task UpdateMachine_WithValidAlias_ReturnsOk()
    {
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "UpdateTestMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await client.PatchAsJsonAsync($"machines/{created!.Id}", new { alias = "MyLaptop" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var getResponse = await client.GetAsync(GetMachinesEndpoint.Route);
        var machines = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        var machine = machines!.Items.FirstOrDefault(m => m.Id == created.Id);
        machine!.Alias.ShouldBe("MyLaptop");
    }

    [Fact(DisplayName = "MACHINES-UPDATE-003: non-existent machine returns 404")]
    public async Task UpdateMachine_WithNonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PatchAsJsonAsync($"machines/{Guid.NewGuid()}", new { alias = "Fake" });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "MACHINES-DELETE-001: missing token returns 401")]
    public async Task DeleteMachine_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"machines/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MACHINES-DELETE-002: delete machine returns 200")]
    public async Task DeleteMachine_WithValidId_ReturnsOk()
    {
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "DeleteTestMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await client.DeleteAsync($"machines/{created!.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "MACHINES-DELETE-003: non-existent machine returns 404")]
    public async Task DeleteMachine_WithNonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.DeleteAsync($"machines/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "MACHINES-DELETE-004: other user's machine returns 404")]
    public async Task DeleteMachine_WithOtherUsersMachine_ReturnsNotFound()
    {
        var user1 = await factory.CreateAuthorizedClient();
        var user2 = await factory.CreateAuthorizedClient($"{Guid.NewGuid():N}@test.com", "Test@1234!");
        var createResponse = await user1.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "User1Machine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await user2.DeleteAsync($"machines/{created!.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "MACHINES-HEARTBEAT-001: missing token returns 401")]
    public async Task Heartbeat_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(HeartbeatEndpoint.Route, new { name = "TestMachine", status = "Active" });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MACHINES-HEARTBEAT-002: new machine creates entry")]
    public async Task Heartbeat_WithNewMachine_CreatesEntry()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync(HeartbeatEndpoint.Route, new { name = "NewHeartbeatMachine", status = "Active" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HeartbeatResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
        var getResponse = await client.GetAsync(GetMachinesEndpoint.Route);
        var machines = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        var machine = machines!.Items.FirstOrDefault(m => m.Name == "NewHeartbeatMachine");
        machine.ShouldNotBeNull();
        machine.Status.ShouldBe(MachineStatus.Active);
    }

    [Fact(DisplayName = "MACHINES-HEARTBEAT-003: existing machine updates status")]
    public async Task Heartbeat_WithExistingMachine_UpdatesStatus()
    {
        var client = await factory.CreateAuthorizedClient();
        var response1 = await client.PostAsJsonAsync(HeartbeatEndpoint.Route, new { name = "UpdateHeartbeatMachine", status = "Active" });
        var body1 = await response1.Content.ReadFromJsonAsync<HeartbeatResponse>();
        var response2 = await client.PostAsJsonAsync(HeartbeatEndpoint.Route, new { name = "UpdateHeartbeatMachine", status = "Inactive" });
        var body2 = await response2.Content.ReadFromJsonAsync<HeartbeatResponse>();
        body2!.Id.ShouldBe(body1!.Id);
        var getResponse = await client.GetAsync(GetMachinesEndpoint.Route);
        var machines = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        var machine = machines!.Items.FirstOrDefault(m => m.Name == "UpdateHeartbeatMachine");
        machine!.Status.ShouldBe(MachineStatus.Inactive);
    }

    [Fact(DisplayName = "MACHINES-HEARTBEAT-004: heartbeat updates LastReportedOn")]
    public async Task Heartbeat_UpdatesLastReportedOn()
    {
        var client = await factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync(HeartbeatEndpoint.Route, new { name = "LastReportedMachine", status = "Active" });
        await Task.Delay(100);
        await client.PostAsJsonAsync(HeartbeatEndpoint.Route, new { name = "LastReportedMachine", status = "Active" });
        var getResponse = await client.GetAsync(GetMachinesEndpoint.Route);
        var machines = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        var machine = machines!.Items.FirstOrDefault(m => m.Name == "LastReportedMachine");
        machine!.LastReportedOn.ShouldNotBeNull();
    }

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

    [Fact(DisplayName = "MACHINES-RESTART-001: valid machine returns 200")]
    public async Task RestartMachine_WithValidId_ReturnsOk()
    {
        var client = await factory.CreateAuthorizedClient();
        var createResponse = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "RestartTestMachine" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response = await client.PostAsJsonAsync($"machines/{created!.Id}/restart", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RestartMachineResponse>();
        body!.Message.ShouldContain("sent");
    }

    [Fact(DisplayName = "MACHINES-RESTART-002: non-existent machine returns 404")]
    public async Task RestartMachine_WithNonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync($"machines/{Guid.NewGuid()}/restart", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
