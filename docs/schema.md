# Data Contracts

These three shapes are the backbone of the whole system. Extraction, retrieval,
the write-back to the knowledge store, and the evaluation harness are all built
against these — locked here, before any code, so nothing downstream drifts.

## 1. TriageRecord

What the LLM extracts from the customer's raw words at intake. This is deliberately
narrow: it never guesses a diagnosis, only structures what was said and flags what's
worth asking.

```json
{
  "id": "guid, assigned by the system, not the model",
  "rawDescription": "clicking sound when turning, pulls left, started a few days ago",
  "vehicle": {
    "make": "string, optional",
    "model": "string, optional",
    "year": "int, optional",
    "mileage": "int, optional"
  },
  "system": "enum: Engine | Transmission | Brakes | Suspension | Steering | Electrical | HVAC | Exhaust | Tyres | Bodywork | Unknown",
  "faultCategory": "short free-text label, e.g. 'CV joint / driveshaft'",
  "symptomTags": ["clicking-noise", "pulls-left", "worsens-on-turn"],
  "urgency": "enum: Low | Medium | High | SafetyCritical",
  "clarifyingQuestions": [
    "Does it click on both left and right turns, or just one direction?",
    "Any vibration through the steering wheel at speed?"
  ],
  "extractionConfidence": "0.0-1.0, the model's confidence in its own reading of the input — not a diagnosis confidence",
  "createdAt": "ISO 8601 timestamp",
  "createdBy": "adviser identifier"
}
```

**Why `symptomTags` exists alongside embeddings**: a cheap, human-readable keyword
layer that lets retrieval be sanity-checked and debugged without opening a vector
index — and gives a second, independent signal alongside semantic similarity.

**Why urgency is its own gated field, not folded into category**: a
`SafetyCritical` flag (e.g. brake or steering faults) should be able to force a
"see a technician now" response regardless of what retrieval finds — urgency
must never wait on a precedent match.

## 2. RetrievalSuggestion

What comes back after the knowledge store is searched. Built so that "we don't
know" is a first-class, clearly represented outcome — never fudged into a
low-confidence guess.

```json
{
  "matchFound": "bool",
  "matchedJobId": "guid, nullable — null if matchFound is false",
  "matchedFaultSummary": "string, nullable — the past fault description that matched",
  "confirmedFix": "string, nullable — what actually fixed it last time",
  "similarityScore": "0.0-1.0, nullable",
  "similarPastCaseCount": "int — how many past jobs matched, not just the closest one",
  "recommendation": "adviser-facing text — either the precedent-backed suggestion, or an explicit 'no strong precedent found, standard diagnostic path applies'",
  "citedJobIds": ["array of guids — every past job this suggestion is grounded in"]
}
```

**Why `citedJobIds` is an array, not a single ID**: a recommendation should be
traceable to every piece of evidence behind it, not just the top match — this is
what makes the confidence score auditable rather than a black box, and it's the
evaluation harness's main hook for measuring hallucination (does the recommendation
text claim anything the cited jobs don't actually support).

## 3. ResolvedJobRecord

What gets written to the knowledge store once a technician confirms the real
diagnosis and fix. This is the record that makes the system compound over time —
stored as Markdown with frontmatter, so it's human-readable documentation and
machine-retrievable memory in the same file.

```yaml
---
id: guid
createdAt: ISO 8601 timestamp
createdBy: adviser identifier
updatedAt: ISO 8601 timestamp
updatedBy: technician identifier
vehicle:
  make: string
  model: string
  year: int
originalTriage: <the TriageRecord id this job started from>
actualDiagnosis: string
actualFix: string
partsUsed: [array of strings]
labourHours: number
outcomeConfirmed: bool   # did a follow-up confirm the fix actually worked
---

## Fault as described by customer
clicking sound when turning, pulls left, started a few days ago

## Diagnosis
Worn CV joint, driver's side.

## Fix
Replaced CV joint assembly. Repacked with new grease boot.

## Notes
Symptom worsened noticeably on full-lock turns — useful discriminator vs.
wheel bearing noise, which tends to be speed-related rather than turn-related.
```

**Why `outcomeConfirmed` exists**: a fix that was applied but later turned out
wrong (comeback job) shouldn't quietly poison future retrieval as if it were a
trusted precedent. Retrieval should be able to filter to `outcomeConfirmed: true`
only, or at minimum surface unconfirmed matches with a visibly lower confidence
— this is a direct, concrete answer to "a confidently wrong answer is worse than
no answer."

## Matching C# record shapes (for direct use in code)

```csharp
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

public record RetrievalSuggestion(
    bool MatchFound,
    Guid? MatchedJobId,
    string? MatchedFaultSummary,
    string? ConfirmedFix,
    double? SimilarityScore,
    int SimilarPastCaseCount,
    string Recommendation,
    IReadOnlyList<Guid> CitedJobIds);

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
    bool OutcomeConfirmed);

public enum VehicleSystem { Engine, Transmission, Brakes, Suspension, Steering, Electrical, Hvac, Exhaust, Tyres, Bodywork, Unknown }
public enum Urgency { Low, Medium, High, SafetyCritical }
public record VehicleInfo(string? Make, string? Model, int? Year, int? Mileage);
```
