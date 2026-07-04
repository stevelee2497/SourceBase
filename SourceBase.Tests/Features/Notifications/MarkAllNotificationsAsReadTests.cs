using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Notifications;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Notifications;

[EndpointFact(
    Feature = "Notifications",
    Name = "Mark All Notifications As Read",
    Route = "PUT /api/notifications/read-all",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to mark all my notifications as read at once, so that I can quickly clear the unread indicator without clicking each notification individually.",
    Description = new[]
    {
        "Finds all unread notifications belonging to the current user.",
        "Sets `IsRead = true` on all of them in a single update.",
        "Returns `{ success: true }`.",
    })]
public class MarkAllNotificationsAsReadTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-001: unread notifications are marked as read")]
    public async Task MarkAllNotificationsAsRead_WithUnreadNotifications_MarksAllAsRead()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.AddRange(
                new NotificationEntity { UserId = userInfo!.Id, Title = "N1", Message = "msg", IsRead = false, Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty },
                new NotificationEntity { UserId = userInfo.Id, Title = "N2", Message = "msg", IsRead = false, Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty }
            );
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkAllNotificationsAsReadResponse>();
        body!.Success.ShouldBeTrue();

        var unreadResponse = await client.GetAsync($"{GetNotificationsEndpoint.Route}?unreadOnly=true");
        var unreadNotifications = await unreadResponse.Content.ReadFromJsonAsync<PagingResponse<NotificationItem>>();
        unreadNotifications!.Total.ShouldBe(0);
    }

    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-002: no notifications returns 200")]
    public async Task MarkAllNotificationsAsRead_WithNoNotifications_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkAllNotificationsAsReadResponse>();
        body!.Success.ShouldBeTrue();
    }

    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-003: other users' notifications are not affected")]
    public async Task MarkAllNotificationsAsRead_DoesNotAffectOtherUsersNotifications()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var otherUserId = Guid.NewGuid();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.Add(new NotificationEntity { UserId = otherUserId, Title = "Other", Message = "msg", IsRead = false, Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty });
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        var otherUserUnread = await factory.WithDbContextAsync(db =>
            db.Notifications.CountAsync(n => n.UserId == otherUserId && !n.IsRead));
        otherUserUnread.ShouldBe(1);
    }

    [Fact(DisplayName = "NOTIF-MARK-ALL-READ-004: unauthenticated request returns 401")]
    public async Task MarkAllNotificationsAsRead_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkAllNotificationsAsReadEndpoint.Route, new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
