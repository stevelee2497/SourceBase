using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Notifications;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Notifications;

public class GetNotificationsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "NOTIF-GET-001: GetNotifications_WithNoNotifications_ReturnsEmptyList")]
    public async Task GetNotifications_WithNoNotifications_ReturnsEmptyList()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(GetNotificationsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetNotificationsResponse>();
        body!.Items.Should().NotBeNull();
        body.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact(DisplayName = "NOTIF-GET-002: GetNotifications_WithExistingNotifications_ReturnsNotifications")]
    public async Task GetNotifications_WithExistingNotifications_ReturnsNotifications()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.Add(new NotificationEntity { UserId = userInfo!.Id, Title = "Test Title", Message = "Test Message" });
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await client.GetAsync(GetNotificationsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetNotificationsResponse>();
        body!.Items.Should().Contain(n => n.Title == "Test Title" && n.Message == "Test Message");
    }

    [Fact(DisplayName = "NOTIF-GET-003: GetNotifications_WithUnreadOnlyFilter_ReturnsOnlyUnread")]
    public async Task GetNotifications_WithUnreadOnlyFilter_ReturnsOnlyUnread()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.AddRange(
                new NotificationEntity { UserId = userInfo!.Id, Title = "Unread", Message = "msg", IsRead = false },
                new NotificationEntity { UserId = userInfo.Id, Title = "Read", Message = "msg", IsRead = true }
            );
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await client.GetAsync($"{GetNotificationsEndpoint.Route}?unreadOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetNotificationsResponse>();
        body!.Items.Should().OnlyContain(n => !n.IsRead);
    }

    [Fact(DisplayName = "NOTIF-GET-004: GetNotifications_WithPagination_ReturnsCorrectPage")]
    public async Task GetNotifications_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        await factory.WithDbContextAsync(async db =>
        {
            for (var i = 0; i < 5; i++)
                db.Notifications.Add(new NotificationEntity { UserId = userInfo!.Id, Title = $"Paged_{i}", Message = "msg" });
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await client.GetAsync($"{GetNotificationsEndpoint.Route}?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetNotificationsResponse>();
        body!.Items.Should().HaveCount(2);
        body.Limit.Should().Be(2);
        body.Page.Should().Be(1);
    }

    [Fact(DisplayName = "NOTIF-GET-005: GetNotifications_WithoutAuth_ReturnsUnauthorized")]
    public async Task GetNotifications_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetNotificationsEndpoint.Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
