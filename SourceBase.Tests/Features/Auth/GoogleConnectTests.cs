using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Google Connect Prepare",
    Route = "POST /api/auth/google/connect/prepare",
    Auth = "Authorized",
    UseCase = "As an authenticated user, I want to link my account to Google by preparing a state token and initiating the OAuth dance.",
    Description = new[]
    {
        "POST /api/auth/google/connect/prepare returns a { state } token stored in cache.",
        "Requires Bearer authentication — anonymous access returns 401.",
        "Each call produces a unique state value.",
        "GET /api/auth/google/connect?state={state} initiates the OAuth challenge.",
        "GET /api/auth/google/connect with missing or unknown state returns 400.",
    })]
public class GoogleConnectTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [RequiresRedisFact(DisplayName = "GOOGLE-CONNECT-001: authenticated user gets state token")]
    public async Task PrepareConnect_AuthenticatedUser_ReturnsStateToken()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsync(GooglePrepareConnectEndpoint.Route, null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GooglePrepareConnectResponse>();
        body.ShouldNotBeNull();
        body!.State.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "GOOGLE-CONNECT-002: anonymous access to prepare returns 401")]
    public async Task PrepareConnect_Anonymous_Returns401()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync(GooglePrepareConnectEndpoint.Route, null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [RequiresRedisFact(DisplayName = "GOOGLE-CONNECT-003: each prepare call returns a unique state token")]
    public async Task PrepareConnect_CalledTwice_ReturnsDifferentStates()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var r1 = await client.PostAsync(GooglePrepareConnectEndpoint.Route, null);
        var r2 = await client.PostAsync(GooglePrepareConnectEndpoint.Route, null);

        // Assert
        var state1 = (await r1.Content.ReadFromJsonAsync<GooglePrepareConnectResponse>())!.State;
        var state2 = (await r2.Content.ReadFromJsonAsync<GooglePrepareConnectResponse>())!.State;
        state1.ShouldNotBe(state2);
    }

    [Fact(DisplayName = "GOOGLE-CONNECT-004: GET connect with missing state param returns 400")]
    public async Task Connect_MissingStateParam_Returns400()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GoogleConnectEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GOOGLE-CONNECT-005: GET connect with unknown state returns 400")]
    public async Task Connect_UnknownState_Returns400()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{GoogleConnectEndpoint.Route}?state={Guid.NewGuid():N}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
