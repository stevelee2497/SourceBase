using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SendGrid;
using SendGrid.Helpers.Mail;
using SourceBase.Application.Shared;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.BackgroundServices;

public class EmailConsumerService(AppSettings appSettings, ILogger<EmailConsumerService> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!appSettings.BackgroundJobSettings.Enabled)
        {
            logger.LogInformation("Background jobs are disabled. EmailConsumerService will not run.");
            return;
        }

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
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.Span);
                logger.LogInformation("Received email message: {Message}", body);
                var email = JsonSerializer.Deserialize<EmailEntity>(body) ?? throw new InvalidOperationException("Deserialized null email message");
                await SendViaGridAsync(email, ct);
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

    private async Task SendViaGridAsync(EmailEntity email, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(appSettings.SendGridApiKey))
        {
            logger.LogWarning("SendGrid API key not configured. Email to {To} skipped.", email.To);
            return;
        }

        var client = new SendGridClient(appSettings.SendGridApiKey);
        var from = new EmailAddress(appSettings.SendGridAccountOwner);
        var to = new EmailAddress(email.To);
        var msg = MailHelper.CreateSingleEmail(from, to, email.Subject, email.Body, email.Body);
        var response = await client.SendEmailAsync(msg, ct);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Email sent to {To} via SendGrid", email.To);
            return;
        }

        var detail = await response.Body.ReadAsStringAsync(ct);
        logger.LogError("SendGrid rejected email to {To}: {Status} — {Detail}", email.To, response.StatusCode, detail);
        throw new InvalidOperationException($"SendGrid error: {response.StatusCode}");
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        await base.StopAsync(ct);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
