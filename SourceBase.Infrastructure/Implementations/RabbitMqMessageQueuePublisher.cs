using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class RabbitMqMessageQueuePublisher(AppSettings appSettings, ILogger<RabbitMqMessageQueuePublisher> logger) : IMessageQueuePublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task PublishAsync<T>(string queue, T message, CancellationToken ct = default)
    {
        var channel = await GetChannelAsync(ct);
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties { Persistent = true };
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queue, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
        logger.LogInformation("Published message to queue '{Queue}'", queue);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _lock.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            var settings = appSettings.RabbitMq;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
