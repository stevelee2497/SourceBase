using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Machines;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Machines;

[EndpointFact(
    Feature = "Machines",
    Name = "Heartbeat (Upsert Machine)",
    Route = "POST /api/machines/heartbeat",
    Auth = "Required",
    UseCase = "As a desktop client, I want to report my status periodically (Active/Inactive/Maintenance).",
    Description = new[]
    {
        "Desktop client sends `name` (unique per user) and `status`.",
        "If machine doesn't exist, creates it; otherwise updates Status and LastReportedOn.",
        "Enables auto-registration: first heartbeat creates the machine entry.",
    })]
public class HeartbeatTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
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
}
