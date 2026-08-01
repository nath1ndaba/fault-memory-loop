using FaultMemoryLoop.Domain.Common;
using FaultMemoryLoop.Domain.ValueObjects;

namespace FaultMemoryLoop.Domain.Entities;

/// <summary>
/// Written to the knowledge store once a technician confirms the real
/// diagnosis and fix. Stored as Markdown with this as YAML frontmatter, so
/// the same file is both human-readable documentation and machine-retrievable
/// memory.
///
/// OutcomeConfirmed exists so a fix that later turned out wrong (a comeback
/// job) doesn't quietly poison future retrieval as if it were trusted
/// precedent — retrieval should filter on this, or at minimum surface
/// unconfirmed matches with visibly lower confidence.
/// </summary>
public record ResolvedJobRecord(
    Guid Id,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy,
    VehicleInfo Vehicle,
    Guid OriginalTriageId,
    string ActualDiagnosis,
    string ActualFix,
    IReadOnlyList<string> PartsUsed,
    double LabourHours,
    bool OutcomeConfirmed) : AuditableEntity(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);
