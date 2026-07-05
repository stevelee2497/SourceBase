using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Machines;
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
        "Client sends `name` (required, unique per user).",
        "Returns the new machine's `Id`.",
        "Machine is auto-assigned Status=Inactive and LastReportedOn=null until first heartbeat.",
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
}
