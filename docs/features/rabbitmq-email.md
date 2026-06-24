# RabbitMQ Email Queue

## Use case

Decouple email sending from the API request lifecycle. Instead of calling SendGrid synchronously in the API, the API publishes an `EmailMessage` to a RabbitMQ queue. A dedicated `SourceBase.EmailWorker` service consumes the queue and sends emails via SendGrid asynchronously.

## Components

| Component | Responsibility |
|---|---|
| `IEmailHelper` | Interface used by handlers (unchanged API surface) |
| `EmailPublisher` | Saves `EmailEntity` to DB, publishes `EmailMessage` to RabbitMQ |
| `IMessageQueuePublisher` | Abstraction over RabbitMQ publishing |
| `RabbitMqMessageQueuePublisher` | Infrastructure implementation — opens channel, serializes to JSON, publishes |
| `SourceBase.EmailWorker` | Worker Service — subscribes to queue, deserializes message, sends via SendGrid |

## Flow

```
API Handler
  → IEmailHelper.SendEmailAsync(to, subject, body)
      → Save EmailEntity to DB (audit trail)
      → IMessageQueuePublisher.PublishAsync("email", EmailMessage)
          → RabbitMQ queue: "email"
              → EmailConsumerService (worker)
                  → SendGrid API
```

## Message shape

```csharp
public record EmailMessage(string To, string Subject, string Body);
```

Serialized as JSON, published to the default exchange with routing key `email`.

## Queue design

- **Exchange:** default (direct, pre-declared)
- **Queue:** `email` (durable)
- **Persistence:** messages marked persistent (`IBasicProperties.Persistent = true`)
- **Ack:** worker sends manual `BasicAck` after successful SendGrid response; `BasicNack` (no requeue) on unrecoverable failure

## Settings

`AppSettings.RabbitMq`:

| Field | Default (dev) |
|---|---|
| `Host` | `localhost` |
| `Port` | `5672` |
| `UserName` | `guest` |
| `Password` | `guest` |
| `QueueName` | `email` |

## Failure modes

| Scenario | Behaviour |
|---|---|
| RabbitMQ unreachable at publish time | `RabbitMqMessageQueuePublisher` throws; handler propagates 500; email entity already saved to DB for audit |
| SendGrid fails in worker | Log error, `BasicNack` without requeue (avoid poison-message loop) |
| Worker restarts mid-consume | Unacked messages are re-delivered by RabbitMQ (at-least-once delivery) |

## DB impact

No migration needed — `EmailEntity` already exists and continues to be written by `EmailPublisher` on the API side.

## docker-compose additions

- `rabbitmq` service: `rabbitmq:3-management-alpine`, ports `5672` (AMQP) and `15672` (management UI)
- `sourcebase-email-worker` service: built from `Dockerfile.emailworker`
