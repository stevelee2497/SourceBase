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
    Name = "Update Machine",
    Route = "PATCH /api/machines/{id}",
    Auth = "Required",
    UseCase = "As a user, I want to update my machine's alias (friendly name).",
    Description = new[]
    {
        "Client sends optional `alias` field.",
        "Only provided fields are updated; omitted fields preserve existing values.",
        "Returns 200 on success, 404 if machine not found or owned by another user.",
    })]
public class UpdateMachineTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
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
}
