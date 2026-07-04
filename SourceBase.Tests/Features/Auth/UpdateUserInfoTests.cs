using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.TodoLists;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Update User Info",
    Route = "PUT /api/auth/info",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to update my profile information (first name, last name, phone number), so that I can keep my personal details current.",
    Description = new[]
    {
        "Client sends `firstName`, `lastName`, and/or `phoneNumber` with a valid access token.",
        "The server loads the current user and updates the provided fields.",
        "Returns the user's `id`.",
    })]
public class UpdateUserInfoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "UPDATE-INFO-001: missing token returns 401")]
    public async Task UpdateUserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName = "John",
            lastName = "Doe",
            phoneNumber = (string?)null,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "UPDATE-INFO-002: valid data returns 200")]
    public async Task UpdateUserInfo_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName = "Admin",
            lastName = "User",
            phoneNumber = "0123456789",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateUserInfoResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "UPDATE-INFO-003: changes reflected in subsequent get")]
    public async Task UpdateUserInfo_ChangesReflectedInGetUserInfo()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var firstName = $"First_{Guid.NewGuid():N}";
        var lastName = $"Last_{Guid.NewGuid():N}";
        var phoneNumber = "0987654321";

        // Act
        await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName,
            lastName,
            phoneNumber,
        });

        // Assert
        var response = await client.GetAsync(GetUserInfoEndpoint.Route);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.ShouldNotBeNull();
        body!.FirstName.ShouldBe(firstName);
        body.LastName.ShouldBe(lastName);
        body.PhoneNumber.ShouldBe(phoneNumber);
    }

    [Fact(DisplayName = "UPDATE-INFO-005: valid todo list id sets default")]
    public async Task UpdateUserInfo_WithValidTodoListId_SetsDefault()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var listResponse = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = $"List_{Guid.NewGuid():N}" });
        var list = await listResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            defaultTodoListId = list!.Id,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var info = await client.GetFromJsonAsync<GetUserInfoResponse>(GetUserInfoEndpoint.Route);
        info!.DefaultTodoListId.ShouldBe(list.Id);
    }

    [Fact(DisplayName = "UPDATE-INFO-006: null todo list id does not clear default")]
    public async Task UpdateUserInfo_WithNullTodoListId_DoesNotClearDefault()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var listResponse = await client.PostAsJsonAsync(CreateTodoListEndpoint.Route, new { name = $"List_{Guid.NewGuid():N}" });
        var list = await listResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();
        await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new { defaultTodoListId = list!.Id });

        // Act — null is treated as absent (partial update), so the field is not cleared
        var response = await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            defaultTodoListId = (Guid?)null,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var info = await client.GetFromJsonAsync<GetUserInfoResponse>(GetUserInfoEndpoint.Route);
        info!.DefaultTodoListId.ShouldBe(list.Id);
    }

    [Fact(DisplayName = "UPDATE-INFO-007: partial update does not overwrite omitted fields")]
    public async Task UpdateUserInfo_PartialUpdate_DoesNotOverwriteOmittedFields()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new { phoneNumber = "0111222333" });

        // Act — send only firstName, phoneNumber should remain unchanged
        var response = await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new { firstName = "PartialFirst" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var info = await client.GetFromJsonAsync<GetUserInfoResponse>(GetUserInfoEndpoint.Route);
        info!.FirstName.ShouldBe("PartialFirst");
        info.PhoneNumber.ShouldBe("0111222333");
    }

    [Fact(DisplayName = "UPDATE-INFO-004: phone number too long returns 400")]
    public async Task UpdateUserInfo_WithPhoneNumberTooLong_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PatchAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName = "Admin",
            lastName = "User",
            phoneNumber = new string('1', 21),
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
