namespace Shared.Domain.Primitives;

// Extends Entity with audit fields that every table in our DB has.
// The infrastructure layer (EF interceptor) fills these in automatically —
// domain code never has to set CreatedAt or UpdatedAt manually.
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Who created/updated — nullable because some records are created by the system (e.g. jobs).
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete: we never physically delete rows in production.
    // Reason: audit trail, referential integrity, ability to restore accidentally deleted data.
    // EF global query filters will automatically exclude IsDeleted = true from all queries.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Optimistic concurrency token.
    // If two requests read the same row and both try to update it,
    // the second one will get a DbUpdateConcurrencyException instead of silently overwriting.
    // Example: two hosts try to block the same dates at the same time.
    public uint Version { get; set; }

    protected AuditableEntity(Guid id) : base(id) { }
}
