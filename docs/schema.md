# Data Contracts

These shapes are the backbone of the whole system. Extraction, retrieval,
the write-back to the knowledge store, auth, and the evaluation harness are
all built against these.

## 1. TriageRecord

What the LLM extracts from the customer's raw words at intake. This is
deliberately narrow: it never guesses a diagnosis, only structures what was
said and flags what's worth asking.

```json
{
  "id": "guid, assigned by the system, not the model",
  "rawDescription": "clicking sound when turning, pulls left, started a few days ago",
  "vehicle": { "make": "string, optional", "model": "string, optional", "year": "int, optional", "mileage": "int, optional" },
  "system": "enum: Engine | Transmission | Brakes | Suspension | Steering | Electrical | Hvac | Exhaust | Tyres | Bodywork | Unknown",
  "faultCategory": "short free-text label",
  "symptomTags": ["clicking-noise", "pulls-left"],
  "urgency": "enum: Low | Medium | High | SafetyCritical",
  "clarifyingQuestions": ["..."],
  "extractionConfidence": "0.0-1.0",
  "createdAt": "ISO 8601 timestamp",
  "createdBy": "adviser identifier"
}
```

`urgency` is independent of retrieval — a `SafetyCritical` fault (brakes,
steering) escalates regardless of whether a precedent match exists.

## 2. RetrievalSuggestion

What comes back after the knowledge store is searched.

```json
{
  "matchFound": "bool",
  "matchedJobId": "guid, nullable",
  "matchedFaultSummary": "string, nullable",
  "confirmedFix": "string, nullable",
  "similarityScore": "0.0-1.0, nullable",
  "similarPastCaseCount": "int",
  "recommendation": "adviser-facing text",
  "citedJobIds": ["every past job this suggestion is grounded in"]
}
```

`matchFound: false` is a first-class outcome, not an error — this is what
lets the eval harness score honest abstention as a real metric.

**Current retrieval implementation**: Jaccard tag overlap on `symptomTags`,
gated to zero unless `system` also matches, threshold 0.5
(`TagOverlapRetrievalService`). This is a deliberate, documented
simplification, not embeddings-based semantic similarity — see Section 4 of
`design.md` for why, and for real evaluation evidence of its limitation
(LLM output variance on `symptomTags` between calls can push an otherwise
correct match below a fixed similarity threshold).

## 3. ResolvedJobRecord

What gets written to the knowledge store once a technician confirms the
real diagnosis and fix. Stored as Markdown with this as YAML frontmatter.

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
system: <VehicleSystem enum — added after an early bug where retrieval
         compared symptom tags against diagnosis prose instead of tags
         against tags; this field plus symptomTags below is the fix>
symptomTags: [array of strings — copied from the triage result at resolve
              time, this is what retrieval actually matches against]
actualDiagnosis: string
actualFix: string
partsUsed: [array of strings]
labourHours: number
outcomeConfirmed: bool
---
```

`outcomeConfirmed` matters because retrieval only trusts jobs where this is
`true` — a fix that later turned out wrong shouldn't quietly poison future
matches. This is enforced in `TagOverlapRetrievalService`, not just
documented.

## 4. Auth contracts

Two independent login paths, both issuing the same shape of token via
`ITokenService`.

```json
// GoogleAuthRequest — POST /api/auth/google
{ "idToken": "Google-issued ID token from the client's own sign-in flow" }

// RegisterRequest — POST /api/auth/register
{ "email": "string", "password": "string, 10+ chars" }

// LoginRequest — POST /api/auth/login
{ "email": "string", "password": "string" }

// AuthResponse — returned by all three above
{ "token": "JWT", "expiresAt": "ISO 8601 timestamp", "email": "string" }
```

Google path verifies via `GoogleJsonWebSignature.ValidateAsync` (real
signature/issuer/audience check, not a stand-in). Email/password path
checks against a real `Employee` table (EF Core + SQLite), passwords hashed
with PBKDF2 (.NET's built-in `Rfc2898DeriveBytes`, 210,000 iterations).

## 5. Triage submission and job resolution

```json
// TriageRequest — POST /api/triage (requires bearer token)
{ "rawDescription": "string", "vehicle": { ... }, "createdBy": "string" }

// TriageResponse — returned by POST /api/triage
{ "triage": <TriageRecord>, "suggestion": <RetrievalSuggestion> }

// ResolveJobRequest — POST /api/jobs/resolve (requires bearer token)
{
  "originalTriageId": "guid",
  "vehicle": { ... },
  "system": "VehicleSystem enum",
  "symptomTags": ["array of strings"],
  "actualDiagnosis": "string",
  "actualFix": "string",
  "partsUsed": ["array of strings"],
  "labourHours": "number",
  "outcomeConfirmed": "bool",
  "resolvedBy": "string"
}
```

## Matching C# record shapes

```csharp
public record TriageRecord(
    Guid Id, string RawDescription, VehicleInfo? Vehicle, VehicleSystem System,
    string FaultCategory, IReadOnlyList<string> SymptomTags, Urgency Urgency,
    IReadOnlyList<string> ClarifyingQuestions, double ExtractionConfidence,
    DateTimeOffset CreatedAt, string CreatedBy);

public record RetrievalSuggestion(
    bool MatchFound, Guid? MatchedJobId, string? MatchedFaultSummary,
    string? ConfirmedFix, double? SimilarityScore, int SimilarPastCaseCount,
    string Recommendation, IReadOnlyList<Guid> CitedJobIds);

public record ResolvedJobRecord(
    Guid Id, DateTimeOffset CreatedAt, string CreatedBy, DateTimeOffset UpdatedAt,
    string UpdatedBy, VehicleInfo Vehicle, Guid OriginalTriageId, VehicleSystem System,
    IReadOnlyList<string> SymptomTags, string ActualDiagnosis, string ActualFix,
    IReadOnlyList<string> PartsUsed, double LabourHours, bool OutcomeConfirmed)
    : AuditableEntity(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);

public enum VehicleSystem { Engine, Transmission, Brakes, Suspension, Steering, Electrical, Hvac, Exhaust, Tyres, Bodywork, Unknown }
public enum Urgency { Low, Medium, High, SafetyCritical }
public record VehicleInfo(string? Make, string? Model, int? Year, int? Mileage);
```
