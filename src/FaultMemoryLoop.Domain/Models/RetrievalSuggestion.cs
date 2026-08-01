namespace FaultMemoryLoop.Domain.Models;

/// <summary>
/// What comes back after the knowledge store is searched. Lives in Models,
/// not Entities — this is a computed result of a query, it has no identity
/// of its own and is never persisted.
///
/// "We don't know" is a first-class outcome here (MatchFound = false) —
/// never fudged into a low-confidence guess. CitedJobIds ties the
/// recommendation text to every piece of evidence it's actually grounded in,
/// which is what makes the evaluation harness able to check for
/// hallucination.
/// </summary>
public record RetrievalSuggestion(
    bool MatchFound,
    Guid? MatchedJobId,
    string? MatchedFaultSummary,
    string? ConfirmedFix,
    double? SimilarityScore,
    int SimilarPastCaseCount,
    string Recommendation,
    IReadOnlyList<Guid> CitedJobIds);
