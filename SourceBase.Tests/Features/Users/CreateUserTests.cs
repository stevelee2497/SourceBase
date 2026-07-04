using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Users;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

[EndpointFact(
    Feature = "Users",
    Name = "Create User",
    Route = "POST /api/users",
    Auth = "Admin only",
    UseCase = "As an admin, I want to create user accounts on behalf of others, so that I can onboard new users without requiring them to self-register.",
    Description = new[]
    {
        "Admin sends `userName`, `email`, `password`, optional `firstName`, `lastName`, `phoneNumber`, and an optional list of `roles`.",
        "If the username or email is already taken → `400 Bad Request`.",
        "If any specified role does not exist in the database → `400 Bad Request`.",
        "Role names are normalised (trimmed, case-insensitive de-duplicated) before assignment.",
        "A new user is created with a hashed password, an OTP confirmation code, and the requested roles.",
        "A confirmation email is sent to the new user.",
        "Returns the new user's `Id`.",
        "A notification is created for every admin user with title \"New User Registered\" and message containing the new user's email.",
    })]
public class CreateUserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "USERS-CREATE-001: no token returns 401")]
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

    [Fact(DisplayName = "USERS-CREATE-002: non-admin user returns 403")]
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

    [Fact(DisplayName = "USERS-CREATE-003: valid data returns 200")]
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

    [Fact(DisplayName = "USERS-CREATE-004: unknown role returns 400")]
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

    [Fact(DisplayName = "USERS-CREATE-005: mixed valid and invalid roles returns 400")]
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

    [Fact(DisplayName = "USERS-CREATE-006: duplicate email (case-insensitive) returns 400")]
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

    [Fact(DisplayName = "USERS-CREATE-007: roles with whitespace are normalized and deduplicated")]
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

    [Fact(DisplayName = "USERS-CREATE-008: valid data creates notification for all admins")]
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
