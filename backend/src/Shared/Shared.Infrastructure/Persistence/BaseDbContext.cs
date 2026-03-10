using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Application.Abstractions;
using Shared.Domain.Primitives;
using Shared.Infrastructure.Outbox;

namespace Shared.Infrastructure.Persistence;

// BaseDbContext is the foundation all module DbContexts inherit from.
// Every module (UsersDbContext, PropertiesDbContext, etc.) gets:
//   - Soft delete global query filter (automatically excludes deleted rows)
//   - OutboxMessages table (same schema, shared across all modules)
//   - IUnitOfWork implementation (SaveChanges, transactions)
//   - Automatic xmin concurrency token for PostgreSQL optimistic concurrency
public abstract class BaseDbContext(DbContextOptions options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes in the calling assembly.
        // Each module's DbContext calls this with its own assembly.
        // Keeps configuration next to the entity, not scattered in OnModelCreating.
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        // Global query filter: automatically appends WHERE IsDeleted = false to ALL queries.
        // Developers never need to remember to filter soft-deleted records.
        // Use IgnoreQueryFilters() only when intentionally querying deleted records (e.g. admin restore).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            }
        }

        // OutboxMessages table — shared across all module schemas via the jobs schema
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages", "jobs");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.EventType).IsRequired().HasMaxLength(500);
            builder.Property(o => o.Payload).IsRequired();
            builder.HasIndex(o => o.CreatedAt)
                .HasFilter("\"ProcessedAt\" IS NULL")
                .HasDatabaseName("idx_outbox_unprocessed");
        });
    }

    // Builds a lambda: (AuditableEntity e) => !e.IsDeleted
    // Done via reflection because EF needs a typed expression per entity type.
    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type type)
    {
        var param = System.Linq.Expressions.Expression.Parameter(type, "e");
        var prop = System.Linq.Expressions.Expression.Property(param, nameof(AuditableEntity.IsDeleted));
        var condition = System.Linq.Expressions.Expression.Not(prop);
        return System.Linq.Expressions.Expression.Lambda(condition, param);
    }

    // IUnitOfWork implementation
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        await transaction.RollbackAsync(cancellationToken);
    }
}
