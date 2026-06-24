using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SendGrid;
using SendGrid.Helpers.Mail;
using SourceBase.Application.Shared;

namespace SourceBase.EmailWorker;

public class EmailConsumerService(AppSettings appSettings, ILogger<EmailConsumerService> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
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

        await _channel.QueueDeclareAsync(settings.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.Span);
            try
            {
                var message = JsonSerializer.Deserialize<EmailMessage>(body)
                    ?? throw new InvalidOperationException("Deserialized null email message");

                await SendViaGridAsync(message, ct);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process email message. Message will not be requeued.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
            }
        };

        await _channel.BasicConsumeAsync(settings.QueueName, autoAck: false, consumer: consumer, cancellationToken: ct);
        logger.LogInformation("Email consumer started on queue '{Queue}'", settings.QueueName);

        await Task.Delay(Timeout.Infinite, ct);
    }

    private async Task SendViaGridAsync(EmailMessage message, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(appSettings.SendGridApiKey))
        {
            logger.LogWarning("SendGrid API key is not configured. Email to {To} not dispatched.", message.To);
            return;
        }

        var client = new SendGridClient(appSettings.SendGridApiKey);
        var from = new EmailAddress(appSettings.SendGridAccountOwner);
        var to = new EmailAddress(message.To);
        var msg = MailHelper.CreateSingleEmail(from, to, message.Subject, message.Body, message.Body);
        var response = await client.SendEmailAsync(msg, ct);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Email sent to {To} via SendGrid", message.To);
            return;
        }

        var detail = await response.Body.ReadAsStringAsync(ct);
        logger.LogError("SendGrid rejected email to {To}: {Status} — {Detail}", message.To, response.StatusCode, detail);
        throw new InvalidOperationException($"SendGrid error: {response.StatusCode}");
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        await base.StopAsync(ct);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
