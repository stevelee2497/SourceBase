using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Users;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class CreateUserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-CREATE-001: CreateUser_WithoutToken_ReturnsUnauthorized")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "USERS-CREATE-002: CreateUser_WithNonAdminUser_ReturnsForbidden")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "USERS-CREATE-003: CreateUser_WithValidData_ReturnsOk")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        body!.Id.ShouldNotBe(Guid.Empty);

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        users!.Items.ShouldContain(x => x.Id == body.Id && x.UserName == userName && x.Email == email && x.Roles.Contains("User"));

        var createdUserFromApi = users.Items.Single(x => x.Id == body.Id);
        createdUserFromApi.EmailConfirmed.ShouldBeFalse();
        var createdUser = await factory.WithDbContextAsync(db => db.Users.SingleAsync(x => x.Id == body.Id));
        createdUser.OtpCode.ShouldNotBeNullOrEmpty();
        createdUser.OtpCodeExpiresOn.ShouldNotBeNull();

        var latestEmail = await factory.WithDbContextAsync(db => db.Emails
            .Where(x => x.To == email)
            .OrderByDescending(x => x.SentOn)
            .FirstOrDefaultAsync());
        latestEmail.ShouldNotBeNull();
        latestEmail!.Subject.ShouldBe("Confirm your email");
        latestEmail.Body.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "USERS-CREATE-004: CreateUser_WithUnknownRole_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "USERS-CREATE-005: CreateUser_WithMixedValidAndInvalidRoles_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "USERS-CREATE-006: CreateUser_WithDuplicateEmailIgnoringCase_ReturnsBadRequest")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "USERS-CREATE-007: CreateUser_WithRolesContainingWhitespace_StoresNormalizedDistinctRoles")]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();

        var usersResponse = await client.GetAsync(GetUsersEndpoint.Route);
        var users = await usersResponse.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>();
        var createdUser = users!.Items.Single(x => x.Id == body!.Id);
        createdUser.Roles.ShouldBe(new[] { "User" });
    }

    [Fact(DisplayName = "USERS-CREATE-008: CreateUser_WithValidData_CreatesNotificationForAllAdmins")]
    public async Task CreateUser_WithValidData_CreatesNotificationForAllAdmins()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var newEmail = $"notif_user_{Guid.NewGuid():N}@test.com";

        var adminIds = await factory.WithDbContextAsync(db => db.Users
            .Where(u => u.Roles.Any(r => r.Name == AppRoles.Admin))
            .Select(u => u.Id)
            .ToListAsync());

        // Act
        await adminClient.PostAsJsonAsync(CreateUserEndpoint.Route, new
        {
            userName = $"notif_user_{Guid.NewGuid():N}",
            email = newEmail,
            password = "Test@1234!",
            roles = new[] { "User" },
        });

        // Assert
        foreach (var adminId in adminIds)
        {
            var notification = await factory.WithDbContextAsync(db => db.Notifications
                .Where(n => n.UserId == adminId)
                .OrderByDescending(n => n.CreatedOn)
                .FirstOrDefaultAsync());
            notification.ShouldNotBeNull();
            notification!.Title.ShouldBe("New User Registered");
            notification.Message.ShouldContain(newEmail);
        }
    }
}
