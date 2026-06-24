using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Tests.Infrastructure;

public class FakeMessageQueuePublisher : IMessageQueuePublisher
{
    public Task PublishAsync<T>(string queue, T message, CancellationToken ct = default) => Task.CompletedTask;
}
