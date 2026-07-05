using Microsoft.AspNetCore.SignalR.Client;
using SourceBase.Desktop.Models;
using System.Text.Json;

namespace SourceBase.Desktop.Services;

/// <summary>
/// Listens for machine commands (Shutdown, Restart) via SignalR hub connection to /hubs/notifications.
/// Connects on demand and reconnects automatically. Fire-and-forget pattern — silent no-op on connection errors.
/// </summary>
public sealed class MachineCommandListener : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly Func<AppSettings> _settings;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public event EventHandler<string>? CommandReceived;

    public MachineCommandListener(Func<AppSettings> settings) => _settings = settings;

    /// <summary>Starts the SignalR connection. Safe to call multiple times; no-op if already connected.</summary>
    public async Task StartAsync()
    {
        if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting })
            return;

        var s = _settings();
        if (string.IsNullOrWhiteSpace(s.ApiBaseUrl) || string.IsNullOrWhiteSpace(s.ApiToken))
            return;

        try
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var hubUrl = $"{s.ApiBaseUrl.TrimEnd('/')}/hubs/notifications";
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(s.ApiToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string>("MachineCommandEvent", commandType =>
            {
                CommandReceived?.Invoke(this, commandType);
            });

            await _connection.StartAsync();
        }
        catch { /* silent fail */ }
    }

    /// <summary>Stops the SignalR connection and cleans up resources.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
