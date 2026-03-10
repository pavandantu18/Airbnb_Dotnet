using MediatR;

namespace Shared.Domain.Primitives;

// Domain events represent something that HAPPENED in the domain — past tense.
// Examples: UserRegisteredEvent, BookingConfirmedEvent, PaymentCapturedEvent
//
// Why events instead of calling services directly?
// - Decoupling: the Bookings module doesn't need to know about Notifications.
//   It just raises BookingConfirmed, and the Notifications module listens.
// - Single Responsibility: each handler does one thing.
// - Testability: you can test that the right events are raised without testing side effects.
//
// INotification comes from MediatR — it makes every domain event
// dispatchable through the MediatR pipeline via INotificationHandler<T>.
public interface IDomainEvent : INotification
{
    // Every event carries a unique ID for idempotency (safe to process twice)
    // and a timestamp for ordering.
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
