using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared.Models;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations;

public class EmailPublisher(IDbContext dbContext, IDateTime dateTime, IMessageQueuePublisher messageQueuePublisher, AppSettings appSettings, ILogger<EmailPublisher> logger) : IEmailHelper
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("Queuing email to {To} with subject {Subject}", to, subject);

        dbContext.Emails.Add(new EmailEntity { To = to, Subject = subject, Body = body, SentOn = dateTime.UtcNow });
        await dbContext.SaveChangesAsync(default);

        await messageQueuePublisher.PublishAsync(appSettings.RabbitMq.QueueName, new EmailMessage(to, subject, body));
    }
}
