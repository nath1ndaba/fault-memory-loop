using FaultMemoryLoop.Domain.Enums;
using FaultMemoryLoop.Domain.ValueObjects;

namespace FaultMemoryLoop.Application.Contracts;

public record ResolveJobRequest(
    Guid OriginalTriageId,
    VehicleInfo Vehicle,
    VehicleSystem System,
    List<string> SymptomTags,
    string ActualDiagnosis,
    string ActualFix,
    List<string> PartsUsed,
    double LabourHours,
    bool OutcomeConfirmed,
    string ResolvedBy);