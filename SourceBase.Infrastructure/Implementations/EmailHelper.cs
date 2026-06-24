using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations;

public class EmailHelper(IDbContext dbContext, IMessageQueuePublisher messageQueuePublisher, AppSettings appSettings, ILogger<EmailHelper> logger) : IEmailHelper
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("Queuing email to {To} with subject {Subject}", to, subject);

        var email = new EmailEntity { To = to, Subject = subject, Body = body, SentOn = DateTime.UtcNow };
        dbContext.Emails.Add(email);
        await dbContext.SaveChangesAsync(default);

        await messageQueuePublisher.PublishAsync(appSettings.RabbitMq.QueueName, email);
    }
}
