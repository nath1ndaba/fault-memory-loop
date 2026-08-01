namespace FaultMemoryLoop.Domain.Common;

/// <summary>
/// Base fields every persisted entity carries: Id, CreatedAt, CreatedBy,
/// UpdatedAt, UpdatedBy. Kept as a record with a `with`-friendly shape rather
/// than a mutable base class, since entities in this domain are immutable
/// throughout.
/// </summary>
public abstract record AuditableEntity(
    Guid Id,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
