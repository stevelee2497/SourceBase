using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Api.Features.Auth;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

public class UpdateUserInfoTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task UpdateUserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName = "John",
            lastName = "Doe",
            phoneNumber = (string?)null,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserInfo_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName = "Admin",
            lastName = "User",
            phoneNumber = "0123456789",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateUserInfoResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateUserInfo_ChangesReflectedInGetUserInfo()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var firstName = $"First_{Guid.NewGuid():N}";
        var lastName = $"Last_{Guid.NewGuid():N}";
        var phoneNumber = "0987654321";

        // Act
        await client.PutAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName,
            lastName,
            phoneNumber,
        });

        // Assert
        var response = await client.GetAsync(GetUserInfoEndpoint.Route);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserInfoResponse>();
        body.Should().NotBeNull();
        body!.FirstName.Should().Be(firstName);
        body.LastName.Should().Be(lastName);
        body.PhoneNumber.Should().Be(phoneNumber);
    }

    [Fact]
    public async Task UpdateUserInfo_WithPhoneNumberTooLong_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserInfoEndpoint.Route, new
        {
            firstName = "Admin",
            lastName = "User",
            phoneNumber = new string('1', 21),
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
