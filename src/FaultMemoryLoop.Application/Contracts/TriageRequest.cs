using FaultMemoryLoop.Domain.ValueObjects;

namespace FaultMemoryLoop.Application.Contracts;

/// <summary>
/// What the adviser actually submits at the counter — the customer's own
/// words, plus whatever vehicle context is already on file.
/// </summary>
public record TriageRequest(string RawDescription, VehicleInfo? Vehicle, string CreatedBy);
