using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Machines;
using SourceBase.Application.Shared;
using SourceBase.Domain.Entities;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Machines;

[EndpointFact(
    Feature = "Machines",
    Name = "Create Machine",
    Route = "POST /api/machines",
    Auth = "Required",
    UseCase = "As a desktop client owner, I want to register my machine with the platform, so that I can manage it remotely.",
    Description = new[]
    {
        "Client sends `name` (required, unique per user) and optionally `status` for upsert semantics.",
        "If machine doesn't exist, creates it with Status=Active (or provided status) and LastReportedOn=null (or now if status provided).",
        "If machine exists, updates its status and LastReportedOn when status is provided.",
        "Returns the machine's `Id`.",
    })]
public class CreateMachineTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
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

    [Fact(DisplayName = "MACHINES-CREATE-004: new machine with status creates entry")]
    public async Task CreateMachine_WithNewMachineAndStatus_CreatesEntry()
    {
        var client = await factory.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "NewHeartbeatMachine", status = "Active" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateMachineResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
        var getResponse = await client.GetAsync(GetMachinesEndpoint.Route);
        var machines = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        var machine = machines!.Items.FirstOrDefault(m => m.Name == "NewHeartbeatMachine");
        machine.ShouldNotBeNull();
        machine.Status.ShouldBe(MachineStatus.Active);
    }

    [Fact(DisplayName = "MACHINES-CREATE-005: existing machine with status updates entry")]
    public async Task CreateMachine_WithExistingMachineAndStatus_UpdatesEntry()
    {
        var client = await factory.CreateAuthorizedClient();
        var response1 = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "UpdateHeartbeatMachine", status = "Active" });
        var body1 = await response1.Content.ReadFromJsonAsync<CreateMachineResponse>();
        var response2 = await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "UpdateHeartbeatMachine", status = "Inactive" });
        var body2 = await response2.Content.ReadFromJsonAsync<CreateMachineResponse>();
        body2!.Id.ShouldBe(body1!.Id);
        var getResponse = await client.GetAsync(GetMachinesEndpoint.Route);
        var machines = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        var machine = machines!.Items.FirstOrDefault(m => m.Name == "UpdateHeartbeatMachine");
        machine!.Status.ShouldBe(MachineStatus.Inactive);
    }

    [Fact(DisplayName = "MACHINES-CREATE-006: machine with status updates LastReportedOn")]
    public async Task CreateMachine_WithStatus_UpdatesLastReportedOn()
    {
        var client = await factory.CreateAuthorizedClient();
        await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "LastReportedMachine", status = "Active" });
        await Task.Delay(100);
        await client.PostAsJsonAsync(CreateMachineEndpoint.Route, new { name = "LastReportedMachine", status = "Active" });
        var getResponse = await client.GetAsync(GetMachinesEndpoint.Route);
        var machines = await getResponse.Content.ReadFromJsonAsync<PagingResponse<GetMachineResponse>>();
        var machine = machines!.Items.FirstOrDefault(m => m.Name == "LastReportedMachine");
        machine!.LastReportedOn.ShouldNotBeNull();
    }
}

