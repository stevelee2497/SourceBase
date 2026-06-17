using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Users;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class UpdateUserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-UPDATE-001: UpdateUser_WithoutToken_ReturnsUnauthorized")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-UPDATE-002: UpdateUser_WithNonAdminUser_ReturnsForbidden")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "USERS-UPDATE-003: UpdateUser_WithValidData_ReturnsOk")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        body!.Id.ShouldBe(createBody.Id);

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        var updatedUser = users!.Items.Single(x => x.Id == createBody.Id);
        updatedUser.UserName.ShouldBe(managedUserName);
        updatedUser.Email.ShouldBe(updatedEmail);
        updatedUser.EmailConfirmed.ShouldBeFalse();
        updatedUser.Roles.ShouldContain("Admin");
        updatedUser.Roles.ShouldContain("User");
    }

    [Fact(DisplayName = "USERS-UPDATE-004: UpdateUser_WithUnknownRole_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "USERS-UPDATE-005: UpdateUser_WithDuplicateEmailIgnoringCase_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "USERS-UPDATE-006: UpdateUser_WithEmailChange_RequiresReconfirmationAndIssuesOtp")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var usersResponse = await adminClient.GetAsync($"{GetUsersEndpoint.Route}?limit=100");
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        var user = users!.Items.Single(x => x.Id == createBody.Id);
        user.Email.ShouldBe(updatedEmail);
        user.EmailConfirmed.ShouldBeFalse();
        var userEntity = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == createBody.Id));
        userEntity.OtpCode.ShouldNotBeNullOrEmpty();
        userEntity.OtpCodeExpiresOn.ShouldNotBeNull();

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == updatedEmail)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());
        latestEmail.ShouldNotBeNull();
        latestEmail!.Subject.ShouldBe("Confirm your email");
        latestEmail.Body.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "USERS-UPDATE-007: UpdateUser_WithRoleChange_InvalidatesUsersExistingToken")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var getInfoResponse = await targetClient.GetAsync(GetUserInfoEndpoint.Route);
        getInfoResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-UPDATE-008: UpdateUser_WithEmptyId_ReturnsBadRequest")]
    public async Task UpdateUser_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(UpdateUserEndpoint.Route.WithId(Guid.Empty), new
        {
            email = $"test_{Guid.NewGuid():N}@test.com",
            firstName = "Test",
            lastName = "User",
            roles = new[] { "User" },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
