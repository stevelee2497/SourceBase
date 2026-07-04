using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Notifications;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Notifications;

[EndpointFact(
    Feature = "Notifications",
    Name = "Mark Notification As Read",
    Route = "PUT /api/notifications/{id}/read",
    Auth = "Required",
    UseCase = "As an authenticated user, I want to mark a specific notification as read by clicking on it, so that I can track which notifications I have already seen.",
    Description = new[]
    {
        "Client provides the notification `id` as a route parameter.",
        "If the notification does not exist or belongs to a different user → `404 Not Found`.",
        "Sets `IsRead = true` on the notification record.",
        "Returns `{ success: true }`.",
    })]
public class MarkNotificationAsReadTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "NOTIF-MARK-READ-001: valid id returns 200")]
    public async Task MarkNotificationAsRead_WithValidId_ReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        var notifId = await factory.WithDbContextAsync(async db =>
        {
            var n = new NotificationEntity { UserId = userInfo!.Id, Title = "Unread", Message = "msg", IsRead = false, Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty };
            db.Notifications.Add(n);
            await db.SaveChangesAsync();
            return n.Id;
        });

        // Act
        var response = await client.PutAsJsonAsync(MarkNotificationAsReadEndpoint.Route.Replace("{id}", notifId.ToString()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkNotificationAsReadResponse>();
        body!.Success.ShouldBeTrue();

        var notificationsResponse = await client.GetAsync($"{GetNotificationsEndpoint.Route}?limit=100");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<PagingResponse<NotificationItem>>();
        var notif = notifications!.Items.Single(n => n.Id == notifId);
        notif.IsRead.ShouldBeTrue();
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-002: non-existent id returns 404")]
    public async Task MarkNotificationAsRead_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkNotificationAsReadEndpoint.Route.Replace("{id}", Guid.NewGuid().ToString()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-003: other user's notification returns 404")]
    public async Task MarkNotificationAsRead_WithOtherUsersNotification_ReturnsNotFound()
    {
        // Arrange
        var adminClient = await factory.CreateAuthorizedClient();
        var anotherUserId = Guid.NewGuid();

        var notifId = await factory.WithDbContextAsync(async db =>
        {
            var n = new NotificationEntity { UserId = anotherUserId, Title = "Other", Message = "msg", IsRead = false, Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty };
            db.Notifications.Add(n);
            await db.SaveChangesAsync();
            return n.Id;
        });

        // Act
        var response = await adminClient.PutAsJsonAsync(MarkNotificationAsReadEndpoint.Route.Replace("{id}", notifId.ToString()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-004: already read notification returns 200")]
    public async Task MarkNotificationAsRead_AlreadyRead_StillReturnsOk()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();
        var userInfoResponse = await client.GetAsync(GetUserInfoEndpoint.Route);
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GetUserInfoResponse>();

        var notifId = await factory.WithDbContextAsync(async db =>
        {
            var n = new NotificationEntity { UserId = userInfo!.Id, Title = "AlreadyRead", Message = "msg", IsRead = true, Event = NotificationEvent.GlobalNotificationEvent, Data = string.Empty };
            db.Notifications.Add(n);
            await db.SaveChangesAsync();
            return n.Id;
        });

        // Act
        var response = await client.PutAsJsonAsync(MarkNotificationAsReadEndpoint.Route.Replace("{id}", notifId.ToString()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkNotificationAsReadResponse>();
        body!.Success.ShouldBeTrue();
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-005: without auth returns 401")]
    public async Task MarkNotificationAsRead_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkNotificationAsReadEndpoint.Route.Replace("{id}", Guid.NewGuid().ToString()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
