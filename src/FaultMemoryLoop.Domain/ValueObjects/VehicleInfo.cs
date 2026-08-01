namespace FaultMemoryLoop.Domain.ValueObjects;

/// <summary>
/// A value object, not an entity — two VehicleInfo instances with the same
/// values are interchangeable, and it has no identity of its own. It always
/// lives attached to an entity (TriageRecord, ResolvedJobRecord), never
/// stored or referenced on its own.
/// </summary>
public record VehicleInfo(string? Make, string? Model, int? Year, int? Mileage);
