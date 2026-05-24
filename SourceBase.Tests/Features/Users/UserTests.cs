using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SourceBase.Api.Features.Todos;
using SourceBase.Api.Features.Users;
using SourceBase.Api.Infrastructure.DbContexts;
using SourceBase.Api.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Users;

public class UserTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userName = $"managed_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@test.com";

        // Act
        var response = await client.PostAsJsonAsync("/api/users", new
        {
            userName,
            email,
            password = "Test@1234!",
            firstName = "Managed",
            lastName = "User",
            phoneNumber = "0123456789",
            roles = new[] { "User" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        body!.Id.Should().NotBeEmpty();
        var users = await GetUsersAsync(client);
        users.Items.Should().ContainSingle(x => x.Id == body.Id && x.UserName == userName && x.Email == email && x.Roles.Contains("User"));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(x => x.Id == body.Id);
        user.EmailConfirmed.Should().BeFalse();
        user.OtpCode.Should().NotBeNullOrEmpty();
        user.OtpCodeExpiresOn.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateUser_WithUnknownRole_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/users", new
        {
            userName = $"invalid_role_{Guid.NewGuid():N}",
            email = $"{Guid.NewGuid():N}@test.com",
            password = "Test@1234!",
            roles = new[] { "UnknownRole" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var user = await CreateManagedUserAsync(client);
        var updatedEmail = $"updated_{Guid.NewGuid():N}@test.com";

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{user.Id}", new
        {
            email = updatedEmail,
            firstName = "Updated",
            lastName = "User",
            phoneNumber = "0987654321",
            roles = new[] { "Admin", "User" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        body!.Id.Should().Be(user.Id);
        var users = await GetUsersAsync(client);
        var updatedUser = users.Items.Single(x => x.Id == user.Id);
        updatedUser.UserName.Should().Be(user.UserName);
        updatedUser.Email.Should().Be(updatedEmail);
        updatedUser.EmailConfirmed.Should().BeFalse();
        updatedUser.Roles.Should().Contain("Admin");
        updatedUser.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task UpdateUser_WithUnknownRole_ReturnsBadRequest()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var user = await CreateManagedUserAsync(client);

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{user.Id}", new
        {
            email = user.Email,
            firstName = "Updated",
            roles = new[] { "UnknownRole" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_WithExistingUser_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var user = await CreateManagedUserAsync(client);

        // Act
        var response = await client.DeleteAsync($"/api/users/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeleteUserResponse>();
        body!.Success.Should().BeTrue();
        var users = await GetUsersAsync(client);
        users.Items.Should().NotContain(x => x.Id == user.Id);
    }

    [Fact]
    public async Task CreateTodo_WithCustomUserName_StoresUserNameInAuditData()
    {
        // Arrange
        var client = factory.CreateClient();
        var userName = $"audit_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234!";
        await client.PostAsJsonAsync("/api/auth/register", new { userName, email, password });
        var code = await factory.GetOtpCode(email);
        await client.PostAsJsonAsync("/api/auth/confirmEmail", new { email, code });
        var token = await factory.GetAccessTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var title = $"Audit_{Guid.NewGuid():N}";

        // Act
        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            date = "2025-10-01",
            title,
            status = "Open",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
        body!.Id.Should().NotBeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var todo = await db.TodoItems.SingleAsync(x => x.Id == body.Id);
        todo.CreatedBy.Should().Be(userName);

        var audit = await db.AuditHistories
            .OrderByDescending(x => x.ActionOn)
            .FirstAsync(x => x.EntityId == body.Id.ToString());

        audit.Author.Should().Be(userName);
    }

    private static async Task<PagingResponse<UserResponse>> GetUsersAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagingResponse<UserResponse>>() ?? throw new InvalidOperationException("Users response was null");
    }

    private async Task<UserResponse> CreateManagedUserAsync(HttpClient client)
    {
        var userName = $"managed_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@test.com";
        var response = await client.PostAsJsonAsync("/api/users", new
        {
            userName,
            email,
            password = "Test@1234!",
            firstName = "Managed",
            lastName = "User",
            roles = new[] { "User" }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        var users = await GetUsersAsync(client);
        return users.Items.Single(x => x.Id == body!.Id);
    }
}
