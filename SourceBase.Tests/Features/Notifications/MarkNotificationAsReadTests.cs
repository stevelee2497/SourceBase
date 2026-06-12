using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SourceBase.Domain.Entities;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Features.Notifications;
using SourceBase.Application.Shared;
using SourceBase.Tests.Infrastructure;
using Xunit;

namespace SourceBase.Tests.Features.Notifications;

public class MarkNotificationAsReadTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact(DisplayName = "NOTIF-MARK-READ-001: MarkNotificationAsRead_WithValidId_ReturnsOk")]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkNotificationAsReadResponse>();
        body!.Success.Should().BeTrue();

        var notificationsResponse = await client.GetAsync($"{GetNotificationsEndpoint.Route}?limit=100");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<PagingResponse<NotificationItem>>();
        var notif = notifications!.Items.Single(n => n.Id == notifId);
        notif.IsRead.Should().BeTrue();
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-002: MarkNotificationAsRead_WithNonExistentId_ReturnsNotFound")]
    public async Task MarkNotificationAsRead_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await factory.CreateAuthorizedClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkNotificationAsReadEndpoint.Route.Replace("{id}", Guid.NewGuid().ToString()), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-003: MarkNotificationAsRead_WithOtherUsersNotification_ReturnsNotFound")]
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
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-004: MarkNotificationAsRead_AlreadyRead_StillReturnsOk")]
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkNotificationAsReadResponse>();
        body!.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "NOTIF-MARK-READ-005: MarkNotificationAsRead_WithoutAuth_ReturnsUnauthorized")]
    public async Task MarkNotificationAsRead_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(MarkNotificationAsReadEndpoint.Route.Replace("{id}", Guid.NewGuid().ToString()), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
