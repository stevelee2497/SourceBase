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
    Name = "Get Machines",
    Route = "GET /api/machines",
    Auth = "Required",
    UseCase = "As a user, I want to list all my registered machines with their current status.",
    Description = new[]
    {
        "Returns a paginated list of the authenticated user's machines.",
        "Each machine includes Name, Alias, Status, LastReportedOn, and Id.",
        "Only returns machines owned by the authenticated user.",
    })]
public class GetMachinesTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
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
}
