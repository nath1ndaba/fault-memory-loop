# Fault Memory Loop

A tool that helps a garage service adviser turn a customer's free-text
description of a car fault into a structured triage record — and, where
the shop has seen a similar fault before, surfaces what actually fixed it
last time, with an honest confidence score instead of a guess.

> **Why this exists, the scenario it's built for, the architecture
> decisions, and an honest note on scope** are documented separately in
> [`docs/design.md`](docs/design.md) — read that for the thinking.
> [`docs/schema.md`](docs/schema.md) documents every request/response
> shape in detail.

## Status

✅ Complete core loop: layered Clean Architecture, dual authentication
(Google OAuth2/OIDC + email/password), real Gemini-backed triage
extraction, retrieval against a growing Markdown knowledge store, and a
real evaluation harness. See `docs/design.md` for an honest note on how
far this went beyond the exercise's stated scope.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`) — one-time,
  for the database migration
- A Google OAuth 2.0 Client ID, to test the Google login path
  (Google Cloud Console → APIs & Services → Credentials → Create
  Credentials → OAuth Client ID → Web application)
- A Gemini API key ([Google AI Studio](https://aistudio.google.com/apikey))

## Setup

1. Clone the repo.
2. Copy `.env.example` to `.env` (inside `src/FaultMemoryLoop.Api/`) and
   fill in your values:
   ```
   JWT_SIGNING_KEY=a-long-random-string-at-least-32-chars
   GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
   GEMINI_API_KEY=your-gemini-key
   GEMINI_MODEL=gemini-3.6-flash
   ```
3. Generate the database migration (one-time):
   ```
   cd src/FaultMemoryLoop.Infrastructure
   dotnet ef migrations add InitialCreate --startup-project ../FaultMemoryLoop.Api
   cd ../..
   ```
4. Run the API:
   ```
   dotnet run --project src/FaultMemoryLoop.Api
   ```
   The SQLite database and `Employees` table are created automatically on
   first run, and Scalar opens automatically.

## Two ways to log in

**Google sign-in** — get a test ID token via
[Google's OAuth 2.0 Playground](https://developers.google.com/oauthplayground/)
against your own Client ID:
```
POST /api/auth/google
{ "idToken": "<the Google ID token>" }
```

**Email + password**:
```
POST /api/auth/register
{ "email": "adviser@example.com", "password": "at-least-10-characters" }

POST /api/auth/login
{ "email": "adviser@example.com", "password": "at-least-10-characters" }
```

Both return the same shape of token. Confirm it works:
```
GET /api/auth/me
Authorization: Bearer <token>
```

## Submitting a fault for triage

Requires a bearer token from either login option.

```
POST /api/triage
Authorization: Bearer <token>
{
  "rawDescription": "clicking sound when turning, pulls slightly left, started a few days ago",
  "vehicle": { "make": "Toyota", "model": "Corolla", "year": 2018 },
  "createdBy": "adviser-jsmith"
}
```

Returns a `TriageRecord` (system, category, urgency, symptom tags,
clarifying questions) plus a `RetrievalSuggestion` — either a cited match
from a past resolved job, or an honest "no precedent found."

## Closing the loop

Once a technician confirms the real diagnosis and fix:

```
POST /api/jobs/resolve
Authorization: Bearer <token>
{
  "originalTriageId": "<id from the triage response>",
  "vehicle": { "make": "Toyota", "model": "Corolla", "year": 2018 },
  "system": "Steering",
  "symptomTags": ["clicking-noise", "pulls-left"],
  "actualDiagnosis": "Worn CV joint, driver's side",
  "actualFix": "Replaced CV joint assembly",
  "partsUsed": ["CV joint assembly"],
  "labourHours": 1.5,
  "outcomeConfirmed": true,
  "resolvedBy": "tech-mreid"
}
```

This writes a new Markdown record to `knowledge-store/jobs/`, which the
*next* matching triage call will find.

## Running the evaluation harness

```bash
dotnet run --project src/FaultMemoryLoop.Eval
```

Runs the real triage + retrieval pipeline (not a mock) against
`eval/test-cases/cases.json`, reporting retrieval precision, abstention
correctness, and hallucination rate. Uses the same `.env` as the API.

## Project structure

```
src/
  FaultMemoryLoop.Domain/          entities, enums, value objects, models
  FaultMemoryLoop.Application/     interfaces, contracts, validators
  FaultMemoryLoop.Infrastructure/  AI services, retrieval, repositories, auth, persistence
  FaultMemoryLoop.Api/             minimal API, endpoints, DI wiring
  FaultMemoryLoop.Eval/            evaluation harness — real scoring, not a placeholder
docs/
  design.md                 problem framing, scenario, architecture, honest scope note
  schema.md                 every data contract in the system
eval/
  test-cases/                held-out fault descriptions used for scoring
knowledge-store/
  jobs/                      resolved job records (Markdown), grows over time
```

## License

Personal exercise submission — not for redistribution.
