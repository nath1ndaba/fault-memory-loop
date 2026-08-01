using FaultMemoryLoop.Domain.Common;
using FaultMemoryLoop.Domain.Enums;
using FaultMemoryLoop.Domain.ValueObjects;

namespace FaultMemoryLoop.Domain.Entities;

public record ResolvedJobRecord(
    Guid Id,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy,
    VehicleInfo Vehicle,
    Guid OriginalTriageId,
    VehicleSystem System,
    IReadOnlyList<string> SymptomTags,
    string ActualDiagnosis,
    string ActualFix,
    IReadOnlyList<string> PartsUsed,
    double LabourHours,
    bool OutcomeConfirmed) : AuditableEntity(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);