namespace FaultMemoryLoop.Domain.Common;

/// <summary>
/// Base entity that provides auditing information for all domain entities.
/// </summary>
public abstract record AuditableEntity(
    Guid Id,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy
);