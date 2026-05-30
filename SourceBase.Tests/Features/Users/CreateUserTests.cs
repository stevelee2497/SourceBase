using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Users;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class CreateUserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "USERS-CREATE-001")]
    [Fact]
    public async Task CreateUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"managed_{Guid.NewGuid():N}",
            email = $"managed_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "USERS-CREATE-002")]
    [Fact]
    public async Task CreateUser_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        // Act
        var response = await nonAdminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"managed_{Guid.NewGuid():N}",
            email = $"managed_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    [Trait("TestCaseId", "USERS-CREATE-003")]
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userName = $"managed_{Guid.NewGuid():N}";
        var email = $"managed_{Guid.NewGuid():N}@test.com";

        // Act
        var response = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName,
            email,
            password = "Test@1234!",
            firstName = "Managed",
            lastName = "User",
            phoneNumber = "0123456789",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        body!.Id.Should().NotBeEmpty();

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        users!.Items.Should().ContainSingle(x => x.Id == body.Id && x.UserName == userName && x.Email == email && x.Roles.Contains("User"));

        var createdUser = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == body.Id));
        createdUser.EmailConfirmed.Should().BeFalse();
        createdUser.OtpCode.Should().NotBeNullOrEmpty();
        createdUser.OtpCodeExpiresOn.Should().NotBeNull();

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());
        latestEmail.Should().NotBeNull();
        latestEmail!.Subject.Should().Be("Confirm your email");
        latestEmail.Body.Should().NotBeNullOrWhiteSpace();
    }
    [Trait("TestCaseId", "USERS-CREATE-004")]
    [Fact]
    public async Task CreateUser_WithUnknownRole_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"invalid_role_{Guid.NewGuid():N}",
            email = $"invalid_role_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "UnknownRole" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Trait("TestCaseId", "USERS-CREATE-005")]
    [Fact]
    public async Task CreateUser_WithMixedValidAndInvalidRoles_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"mixed_roles_{Guid.NewGuid():N}",
            email = $"mixed_roles_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User", "UnknownRole" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Trait("TestCaseId", "USERS-CREATE-006")]
    [Fact]
    public async Task CreateUser_WithDuplicateEmailIgnoringCase_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var existingEmail = $"duplicate_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"duplicate_{Guid.NewGuid():N}",
            email = existingEmail,
            password = "Test@1234!",
            roles = new[] { "User" },
        });

        // Act
        var response = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"duplicate_{Guid.NewGuid():N}",
            email = existingEmail.ToUpperInvariant(),
            password = "Test@1234!",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Trait("TestCaseId", "USERS-CREATE-007")]
    [Fact]
    public async Task CreateUser_WithRolesContainingWhitespace_StoresNormalizedDistinctRoles()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"normalized_roles_{Guid.NewGuid():N}",
            email = $"normalized_roles_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { " User ", "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        var createdUser = users!.Items.Single(x => x.Id == body!.Id);
        createdUser.Roles.Should().Equal("User");
    }
}
