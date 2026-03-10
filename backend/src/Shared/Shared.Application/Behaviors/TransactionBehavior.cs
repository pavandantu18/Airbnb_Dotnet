using MediatR;
using Shared.Application.Abstractions;
using Shared.Application.Messaging;

namespace Shared.Application.Behaviors;

// TransactionBehavior wraps every COMMAND (not queries) in a database transaction.
// This guarantees atomicity: either all DB changes + outbox messages commit together,
// or nothing does.
//
// Why this matters for the outbox pattern:
//   Handler writes:  1) Domain changes to DB   2) OutboxMessage to DB
//   Both happen inside ONE transaction.
//   If the transaction commits → both are saved atomically.
//   If it fails → both are rolled back. No orphaned events, no lost messages.
//
// Queries are excluded — they're read-only and transactions add unnecessary overhead.
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only wrap commands in a transaction — queries skip this entirely
        if (request is not ICommand and not ICommand<TResponse>)
            return await next(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(cancellationToken);
            await unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            throw;
        }
    }
}
