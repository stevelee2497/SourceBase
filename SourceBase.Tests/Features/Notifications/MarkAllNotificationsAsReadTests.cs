using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Notifications;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Notifications;

public class MarkAllNotificationsAsReadTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-001: MarkAllNotificationsAsRead_WithUnreadNotifications_MarksAllAsRead")]
    public async Task MarkAllNotificationsAsRead_WithUnreadNotifications_MarksAllAsRead()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.AddRange(
                new NotificationEntity { UserId = userInfo!.Id, Title = "N1", Message = "msg", IsRead = false },
                new NotificationEntity { UserId = userInfo.Id, Title = "N2", Message = "msg", IsRead = false }
            );
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkAllNotificationsAsReadResponse>();
        body!.Success.Should().BeTrue();

        var unreadResponse = await client.GetAsync($"{GetNotificationsEndpoint.Route}?unreadOnly=true");
        var unreadNotifications = await unreadResponse.Content.ReadFromJsonAsync<GetNotificationsResponse>();
        unreadNotifications!.Total.Should().Be(0);
    }

    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-002: MarkAllNotificationsAsRead_WithNoNotifications_ReturnsOk")]
    public async Task MarkAllNotificationsAsRead_WithNoNotifications_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkAllNotificationsAsReadResponse>();
        body!.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-003: MarkAllNotificationsAsRead_DoesNotAffectOtherUsersNotifications")]
    public async Task MarkAllNotificationsAsRead_DoesNotAffectOtherUsersNotifications()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var otherUserId = Guid.NewGuid();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.Add(new NotificationEntity { UserId = otherUserId, Title = "Other", Message = "msg", IsRead = false });
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        var otherUserUnread = await factory.WithDbContextAsync(db =>
            db.Notifications.CountAsync(n => n.UserId == otherUserId && !n.IsRead));
        otherUserUnread.Should().Be(1);
    }

    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-004: MarkAllNotificationsAsRead_WithoutAuth_ReturnsUnauthorized")]
    public async Task MarkAllNotificationsAsRead_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
