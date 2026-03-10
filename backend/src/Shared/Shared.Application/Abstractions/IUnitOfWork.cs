using Microsoft.EntityFrameworkCore.Storage;

namespace Shared.Application.Abstractions;

// IUnitOfWork abstracts the database transaction from the Application layer.
// The Application layer only depends on this interface — never on EF Core directly.
// This keeps the domain and application layers free of infrastructure concerns,
// and makes it possible to swap the DB implementation without touching business logic.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
}
