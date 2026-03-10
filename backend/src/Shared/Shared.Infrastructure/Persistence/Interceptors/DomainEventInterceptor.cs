using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Domain.Primitives;
using Shared.Infrastructure.Outbox;

namespace Shared.Infrastructure.Persistence.Interceptors;

// After saving domain changes to the DB, this interceptor converts all raised domain events
// into OutboxMessages and saves them in the SAME transaction.
//
// Why after saving, not before?
// The entity must be saved first so its ID exists in the DB before we reference it in an event.
// We use SavedChanges (past tense) — runs after the transaction commits.
//
// The Hangfire outbox processor will pick up these messages and publish them to RabbitMQ.
public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        PublishDomainEventsToOutbox(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        PublishDomainEventsToOutbox(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static void PublishDomainEventsToOutbox(DbContext? context)
    {
        if (context is null) return;

        // Collect all domain events from all tracked entities
        var domainEvents = context.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .SelectMany(e =>
            {
                var events = e.DomainEvents.ToList();
                e.ClearDomainEvents(); // prevent double-publishing on next SaveChanges
                return events;
            })
            .ToList();

        if (domainEvents.Count == 0) return;

        // Convert each domain event to an OutboxMessage row
        var outboxMessages = domainEvents.Select(evt => new OutboxMessage
        {
            Id = evt.EventId,
            EventType = evt.GetType().AssemblyQualifiedName ?? evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt, evt.GetType()),
            CreatedAt = evt.OccurredOn
        }).ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);

        // Note: we do NOT call SaveChanges here.
        // The outbox messages will be saved in the same transaction that's already in flight.
        // The TransactionBehavior commits everything together.
    }
}
