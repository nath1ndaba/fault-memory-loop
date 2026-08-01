using FaultMemoryLoop.Domain.Enums;
using FaultMemoryLoop.Domain.ValueObjects;

namespace FaultMemoryLoop.Domain.Entities;

/// <summary>
/// What the LLM extracts from the customer's raw words at intake.
/// Deliberately narrow: it structures what was said, it never guesses a diagnosis.
/// See docs/schema.md for the full rationale behind each field.
/// </summary>
public record TriageRecord(
    Guid Id,
    string RawDescription,
    VehicleInfo? Vehicle,
    VehicleSystem System,
    string FaultCategory,
    IReadOnlyList<string> SymptomTags,
    Urgency Urgency,
    IReadOnlyList<string> ClarifyingQuestions,
    double ExtractionConfidence,
    DateTimeOffset CreatedAt,
    string CreatedBy);
