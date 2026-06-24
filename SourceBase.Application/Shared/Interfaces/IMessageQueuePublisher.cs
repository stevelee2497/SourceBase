namespace SourceBase.Application.Shared.Interfaces;

public interface IMessageQueuePublisher
{
    Task PublishAsync<T>(string queue, T message, CancellationToken ct = default);
}
