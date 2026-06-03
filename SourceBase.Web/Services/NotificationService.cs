using Microsoft.AspNetCore.SignalR.Client;
using SourceBase.Web.Auth;

namespace SourceBase.Web.Services;

public class NotificationService(BlazorAuthStateProvider auth, IConfiguration configuration) : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl = configuration["ApiBaseUrl"]!.TrimEnd('/') + "/hubs/notifications";

    public List<NotificationResponse> Notifications { get; private set; } = [];
    public int UnreadCount => Notifications.Count(n => !n.IsRead);

    public event Action? OnChange;

    public async Task StartAsync()
    {
        if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting })
            return;

        if (string.IsNullOrWhiteSpace(auth.AccessToken))
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(auth.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<NotificationResponse>("GlobalNotificationEvent", notification =>
        {
            Notifications.Insert(0, notification);
            OnChange?.Invoke();
        });

        await _connection.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public void SetNotifications(List<NotificationResponse> notifications)
    {
        Notifications = notifications;
        OnChange?.Invoke();
    }

    public void MarkReadLocally(Guid id)
    {
        var index = Notifications.FindIndex(n => n.Id == id);
        if (index >= 0)
            Notifications[index] = Notifications[index] with { IsRead = true };
        OnChange?.Invoke();
    }

    public void MarkAllReadLocally()
    {
        Notifications = Notifications.Select(n => n with { IsRead = true }).ToList();
        OnChange?.Invoke();
    }

    public void ClearLocally()
    {
        Notifications.Clear();
        OnChange?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
