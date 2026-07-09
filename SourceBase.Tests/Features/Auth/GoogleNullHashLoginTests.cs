using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Shared;
using SourceBase.Domain.Entities;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Auth;

[EndpointFact(
    Feature = "Auth",
    Name = "Login (Google-only account)",
    Route = "POST /api/auth/login",
    Auth = "Anonymous",
    UseCase = "As a user whose account was created via Google OAuth (no password), I should receive a clear error when I try to sign in with email/password.",
    Description = new[]
    {
        "If the user has PasswordHash = null, Login returns 401 with a descriptive message.",
        "This prevents a null-dereference in VerifyPassword and guides the user to use Google.",
        "Regular users with a password are not affected by this guard.",
    })]
public class GoogleNullHashLoginTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "GOOGLE-LOGIN-001: Google-only account returns 401 with Google sign-in message")]
    public async Task Login_GoogleOnlyAccount_Returns401WithGoogleMessage()
    {
        // Arrange
        var email = $"google_only_{Guid.NewGuid():N}@test.com";
        await factory.WithDbContextAsync(async db =>
        {
            var role = await db.Roles.FirstAsync(r => r.Name == AppRoles.User);
            var user = new UserEntity
            {
                Email = email,
                UserName = $"google_{Guid.NewGuid():N}",
                GoogleId = $"gid_{Guid.NewGuid():N}",
                EmailConfirmed = true,
                PasswordHash = null,
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            user.Roles.Add(role);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return true;
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new { email, password = "any-password" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Google sign-in");
    }

    [Fact(DisplayName = "GOOGLE-LOGIN-002: regular user with password is not affected by null-hash guard — returns 200")]
    public async Task Login_RegularUserWithPassword_Returns200()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginEndpoint.Route, new
        {
            email = WebAppFactory.AdminEmail,
            password = WebAppFactory.AdminPassword,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.ShouldNotBeNullOrEmpty();
    }
}
