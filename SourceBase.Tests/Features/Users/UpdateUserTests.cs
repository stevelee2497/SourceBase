using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Features.Auth;
using SourceBase.Api.Features.Users;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class UpdateUserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Trait("TestCaseId", "USERS-UPDATE-001")]
    [Fact]
    public async Task UpdateUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(Guid.NewGuid()), new
        {
            email = $"updated_{Guid.NewGuid():N}@test.com",
            firstName = "Updated",
            lastName = "User",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Trait("TestCaseId", "USERS-UPDATE-002")]
    [Fact]
    public async Task UpdateUser_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var nonAdminClient = await factory.CreateAuthorizedClient($"non_admin_{Guid.NewGuid():N}@test.com", "Test@1234!");

        var adminClient = await factory.CreateAuthorizedClient();
        var targetEmail = $"target_{Guid.NewGuid():N}@test.com";
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"target_{Guid.NewGuid():N}",
            email = targetEmail,
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await nonAdminClient.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(createBody!.Id), new
        {
            email = targetEmail,
            firstName = "Updated",
            lastName = "User",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    [Trait("TestCaseId", "USERS-UPDATE-003")]
    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var managedUserName = $"managed_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = managedUserName,
            email = $"managed_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        var updatedEmail = $"updated_{Guid.NewGuid():N}@test.com";

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(createBody!.Id), new
        {
            email = updatedEmail,
            firstName = "Updated",
            lastName = "User",
            phoneNumber = "0987654321",
            roles = new[] { "Admin", "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        body!.Id.Should().Be(createBody.Id);

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        var updatedUser = users!.Items.Single(x => x.Id == createBody.Id);
        updatedUser.UserName.Should().Be(managedUserName);
        updatedUser.Email.Should().Be(updatedEmail);
        updatedUser.EmailConfirmed.Should().BeFalse();
        updatedUser.Roles.Should().Contain("Admin");
        updatedUser.Roles.Should().Contain("User");
    }
    [Trait("TestCaseId", "USERS-UPDATE-004")]
    [Fact]
    public async Task UpdateUser_WithUnknownRole_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var managedEmail = $"managed_{Guid.NewGuid():N}@test.com";
        var createResponse = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"managed_{Guid.NewGuid():N}",
            email = managedEmail,
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(createBody!.Id), new
        {
            email = managedEmail,
            firstName = "Updated",
            roles = new[] { "UnknownRole" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Trait("TestCaseId", "USERS-UPDATE-005")]
    [Fact]
    public async Task UpdateUser_WithDuplicateEmailIgnoringCase_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        var existingEmail = $"existing_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"existing_{Guid.NewGuid():N}",
            email = existingEmail,
            password = "Test@1234!",
            roles = new[] { "User" },
        });

        var targetEmail = $"target_{Guid.NewGuid():N}@test.com";
        var targetCreateResponse = await client.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"target_{Guid.NewGuid():N}",
            email = targetEmail,
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var targetCreateBody = await targetCreateResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(targetCreateBody!.Id), new
        {
            email = existingEmail.ToUpperInvariant(),
            firstName = "Updated",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Trait("TestCaseId", "USERS-UPDATE-006")]
    [Fact]
    public async Task UpdateUser_WithEmailChange_RequiresReconfirmationAndIssuesOtp()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var createResponse = await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"email_change_{Guid.NewGuid():N}",
            email = $"email_change_{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "User" },
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        var updatedEmail = $"email_changed_{Guid.NewGuid():N}@test.com";

        // Act
        var response = await adminClient.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(createBody!.Id), new
        {
            email = updatedEmail,
            firstName = "Updated",
            lastName = "User",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == createBody.Id));
        user.Email.Should().Be(updatedEmail);
        user.EmailConfirmed.Should().BeFalse();
        user.OtpCode.Should().NotBeNullOrEmpty();
        user.OtpCodeExpiresOn.Should().NotBeNull();

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == updatedEmail)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());
        latestEmail.Should().NotBeNull();
        latestEmail!.Subject.Should().Be("Confirm your email");
        latestEmail.Body.Should().NotBeNullOrWhiteSpace();
    }
    [Trait("TestCaseId", "USERS-UPDATE-007")]
    [Fact]
    public async Task UpdateUser_WithRoleChange_InvalidatesUsersExistingToken()
    {
        // Arrange
        var targetEmail = $"role_change_{Guid.NewGuid():N}@test.com";
        var targetClient = await factory.CreateAuthorizedClient(targetEmail, "Test@1234!");

        var targetInfoResponse = await targetClient.GetAsync(GetUserInfoEndpoint.Route);
        var targetInfo = await targetInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        var adminClient = await factory.CreateAuthorizedClient();

        // Act
        var response = await adminClient.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(targetInfo!.Id), new
        {
            email = targetEmail,
            roles = new[] { "Admin", "User" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getInfoResponse = await targetClient.GetAsync(GetUserInfoEndpoint.Route);
        getInfoResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
