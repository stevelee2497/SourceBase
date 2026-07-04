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
    Name = "Clear All Notifications",
    Route = "DELETE /api/notifications",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to clear all my notifications, so that I can keep my notification list clean and remove events I no longer care about.",
    Description = new[]
    {
        "Deletes all notifications belonging to the current user.",
        "Returns `{ success: true }`.",
    })]
public class ClearAllNotificationsTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "NOTIF-CLEAR-001: existing notifications are deleted")]
    public async Task ClearAllNotifications_WithExistingNotifications_DeletesAll()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.AddRange(
                new NotificationEntity { UserId = userInfo!.Id, Title = "C1", Message = "msg", Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty },
                new NotificationEntity { UserId = userInfo.Id, Title = "C2", Message = "msg", Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty }
            );
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await client.DeleteAsync(ClearAllNotificationsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClearAllNotificationsResponse>();
        body!.Success.ShouldBeTrue();

        var notificationsResponse = await client.GetAsync(GetNotificationsEndpoint.Route);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<PagingResponse<NotificationItem>>();
        notifications!.Total.ShouldBe(0);
    }

    [Fact(DisplayName = "NOTIF-CLEAR-002: no notifications returns 200")]
    public async Task ClearAllNotifications_WithNoNotifications_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync(ClearAllNotificationsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClearAllNotificationsResponse>();
        body!.Success.ShouldBeTrue();
    }

    [Fact(DisplayName = "NOTIF-CLEAR-003: other users' notifications are not affected")]
    public async Task ClearAllNotifications_DoesNotAffectOtherUsersNotifications()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var otherUserId = Guid.NewGuid();

        await factory.WithDbContextAsync(async db =>
        {
            db.Notifications.Add(new NotificationEntity { UserId = otherUserId, Title = "Other", Message = "msg", Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty });
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        await client.DeleteAsync(ClearAllNotificationsEndpoint.Route);

        // Assert
        var otherUserCount = await factory.WithDbContextAsync(db =>
            db.Notifications.CountAsync(n => n.UserId == otherUserId));
        otherUserCount.ShouldBe(1);
    }

    [Fact(DisplayName = "NOTIF-CLEAR-004: without auth returns 401")]
    public async Task ClearAllNotifications_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(ClearAllNotificationsEndpoint.Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
