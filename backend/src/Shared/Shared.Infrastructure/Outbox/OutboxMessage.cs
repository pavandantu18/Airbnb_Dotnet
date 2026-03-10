namespace Shared.Infrastructure.Outbox;

// The Outbox pattern solves the "dual write" problem:
// When a handler saves data AND needs to publish an event, two separate writes occur.
// If the app crashes between them, the DB is updated but the event is never sent.
//
// Solution: Write the event as a row in the same DB transaction as the domain change.
// A background job (Hangfire) then reads unprocessed outbox messages and publishes them to RabbitMQ.
// This gives us "at-least-once" delivery with idempotent consumers handling duplicates.
//
// Flow:
//   1. Handler saves entity + OutboxMessage in ONE transaction
//   2. Hangfire polls OutboxMessages WHERE ProcessedAt IS NULL
//   3. Publishes to RabbitMQ, marks ProcessedAt = now
//   4. If step 3 fails, Hangfire retries (the message is still unprocessed)
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // The full type name of the domain event, used to deserialize Payload back to the right type.
    // Example: "Users.Domain.Events.UserRegisteredEvent, Users.Domain"
    public string EventType { get; init; } = string.Empty;

    // JSON-serialized domain event. We store as string so we don't need the event type
    // at write time — just serialize whatever event we have.
    public string Payload { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Null = not yet processed. Set when the job successfully publishes to RabbitMQ.
    public DateTime? ProcessedAt { get; set; }

    // If publishing fails, store the error for debugging.
    public string? Error { get; set; }

    // How many times publishing has been attempted. After N retries, alert the dev team.
    public int RetryCount { get; set; }
}
