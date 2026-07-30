# Fault Memory Loop

A small tool that helps a garage service adviser turn a customer's free-text
description of a car fault into a structured triage record — and, where the
shop has seen a similar fault before, surfaces what actually fixed it last
time, with an honest confidence score instead of a guess.

> **Why this exists, the scenario it's built for, and the architecture
> decisions behind it** are documented separately in
> [`docs/design.md`](docs/design.md). This README is deliberately just the
> practical "how to run it" — read the design doc for the thinking.

## Status

🚧 Structure + authentication. Real JWT issuance, Google OAuth2/OIDC
verification, and email/password login (Employee table via EF Core +
SQLite) — two independent ways to get a token, both issuing the same shape
of JWT. No AI yet; that's on hold and lands in its own commit later.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`) — needed once,
  to generate the database migration (see below)
- A Google OAuth 2.0 Client ID, if you want to test the Google login path
  (Google Cloud Console → APIs & Services → Credentials → Create
  Credentials → OAuth Client ID → Web application)

## Setup

1. Clone the repo.
2. Copy `.env.example` to `.env` and fill in a JWT signing key (and a
   Google Client ID if testing that path):
   ```
   JWT_SIGNING_KEY=a-long-random-string-at-least-32-chars
   GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
   ```
3. Generate the database migration (one-time — see
   `src/FaultMemoryLoop.Infrastructure/Migrations/README.md`):
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
   first run.
5. Open the interactive API docs at the URL printed on startup (served via
   Scalar), or hit `GET /health` directly to confirm it's running.

## Two ways to log in

**Option 1 — Google sign-in.** There's no sign-in page yet, so the simplest
way to get a real Google ID token to test with is
[Google's OAuth 2.0 Playground](https://developers.google.com/oauthplayground/):
authorize against your own Client ID, then use the ID token it returns.
```
POST /api/auth/google
{ "idToken": "<the Google ID token>" }
```

**Option 2 — email + password.**
```
POST /api/auth/register
{ "email": "adviser@example.com", "password": "at-least-10-characters" }

POST /api/auth/login
{ "email": "adviser@example.com", "password": "at-least-10-characters" }
```

Both options return the same shape of token. Either way:
```
GET /api/auth/me
Authorization: Bearer <token>
```
confirms the whole chain works end to end.

## Project structure

```
src/
  FaultMemoryLoop.Domain/          entities, enums, value objects — currently empty
  FaultMemoryLoop.Application/     interfaces, contracts, validators — currently empty
  FaultMemoryLoop.Infrastructure/  AI services, repositories, auth — currently empty
  FaultMemoryLoop.Api/             minimal API, endpoints, DI wiring
  FaultMemoryLoop.Eval/            evaluation harness — placeholder
docs/
  design.md                 problem framing, scenario, architecture rationale
  schema.md                 the data contracts the next commits build against
eval/
  test-cases/                will hold the held-out fault descriptions used for scoring
knowledge-store/
  jobs/                      will hold resolved job records (Markdown), once that feature lands
```

## License

Personal exercise submission — not for redistribution.
