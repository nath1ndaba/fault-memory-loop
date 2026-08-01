using FaultMemoryLoop.Domain.Entities;
using FaultMemoryLoop.Domain.ValueObjects;

namespace FaultMemoryLoop.Application.Interfaces;

/// <summary>
/// Turns a customer's raw fault description into a structured TriageRecord.
/// Kept as an interface so the concrete model/provider is a config choice,
/// not something wired throughout the codebase.
/// </summary>
public interface ITriageExtractionService
{
    Task<TriageRecord> ExtractAsync(string rawDescription, VehicleInfo? vehicle, string createdBy, CancellationToken ct = default);
}
