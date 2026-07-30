# Fault Memory Loop
### A self-improving diagnostic memory for garage fault intake
*Design brief — Klipboard AI Software Developer Take-Home Exercise*

## Tech stack at a glance

*Note: this section describes the intended full build. It's being delivered
commit by commit — see the repo's commit history for what's actually
landed at any given point. As of the current commit: structure (Domain /
Application / Infrastructure / Api projects) plus real authentication —
two independent login paths (Google OAuth2/OIDC verification, and email/
password against a real Employee table via EF Core + SQLite), both issuing
the same shape of JWT, protecting `/api/auth/me`. AI extraction is
deliberately on hold and lands in its own later commit.*

**Built (across the full sequence):** .NET 10 · layered Clean Architecture (Domain / Application /
Infrastructure / Api as real projects, `.slnx` solution) · ASP.NET Core
Minimal APIs · Microsoft.Extensions.AI + Gemini · Serilog · FluentValidation ·
rate limiting · Scalar · JWT issuance and validation (adviser-protected
triage endpoint) · consistent `ApiResponse<T>` envelope · base entity audit
fields · a real Markdown-backed job repository · Dockerfile.

**Deliberately deferred, and documented as a production roadmap instead:**
a full external identity provider (Entra ID, Auth0) behind the JWT layer ·
CQRS + MediatR · a generic repository base abstraction · AutoMapper · Scrutor ·
Blazor + MudBlazor frontend (with charts) + Refit · Docker Compose + Render
deployment.

Full reasoning for every choice is in **Section 4** below.

## 0. Picture the counter

A customer walks into the shop. His car has been making a noise. He is not a
mechanic — he says something like:

> "There's a clicking sound when I turn the wheel, and it feels like the car
> pulls to the left a bit. Started a few days ago, gets worse the sharper I
> turn."

The service adviser at the counter has to turn that into something useful in the
next sixty seconds: what's likely wrong, how urgent is it, what should be booked,
and roughly what it'll cost and take. If the adviser is new, or it's a fault type
they haven't seen before, they're guessing — or waiting for a senior tech to
become free just to have a two-minute conversation that decides everything.

Now imagine that same shop has quietly fixed eleven CV-joint faults with almost
identical symptoms over the past year. That knowledge exists — it's just locked
inside whichever technician happened to diagnose those eleven cars. The adviser
at the counter has no way to reach it.

**This is the moment the tool is built for**: the adviser types (or the system
transcribes) what the customer just said, and within seconds gets back not a
generic guess, but "here's what this looked like the last three times, here's
what actually fixed it, here's how confident we are" — or, just as importantly,
an honest "we haven't seen this exact pattern before, here's the standard
diagnostic path." Either way, the adviser walks back to the customer with a real
answer instead of a shrug, and the workshop books the right job with the right
parts on order before the car even goes on the ramp.

Everything below exists to make that sixty-second moment work — reliably, safely,
and better every month the shop is open.

## 1. The problem

When a fault comes in as free text ("car makes a clicking noise when turning, pulls
left"), an experienced technician often recognises the pattern instantly. That
recognition is tribal knowledge — undocumented, held in one person's head, and lost
when that person is unavailable, moves on, or retires. Junior advisers and new hires
re-diagnose problems the shop has effectively already solved before.

Most fault-intake tools stop at classification. This one is designed to make the
garage's own historical knowledge reusable, and to get better every time a job closes
— without anyone having to manually curate a knowledge base.

## 2. The idea

**Fault Memory Loop**: an intake tool that triages incoming fault descriptions *and*
consults a growing, evidence-backed memory of the garage's own past resolved jobs
before offering any suggestion — closing the loop automatically once a job is
confirmed fixed.

### Flow

Walking the counter scenario through the system end to end:

1. **Intake** — the adviser types the customer's own words ("clicking sound when
   turning, pulls left, started a few days ago") straight into the tool. An LLM
   extracts a structured triage record: likely system (steering/suspension/CV
   joint), fault category, urgency, and suggested clarifying questions the
   adviser can ask on the spot ("does it click on both left and right turns, or
   just one?").
2. **Recall** — before suggesting anything beyond basic triage, the system
   searches a local store of past resolved jobs (via embeddings) for similar
   historical faults, ideally filtered by vehicle make/model/system — in this
   case, pulling up the eleven prior CV-joint cases with matching symptoms.
3. **Confidence-gated suggestion** — if a strong precedent exists, surface it: the
   past fault description, the confirmed fix, and a similarity/confidence score,
   with a direct citation to the source record, so the adviser can tell the
   customer "this usually turns out to be X, here's roughly what it involves."
   If no good precedent exists, the system says so explicitly rather than
   guessing — the adviser still gets the standard diagnostic path, just without
   a false sense of certainty.
4. **Close the loop** — once the technician confirms the actual diagnosis and fix
   (in this case, a worn CV joint), it's logged as a new Markdown record in the
   knowledge store. The next customer with the same symptoms benefits
   automatically — the shop's diagnostic memory compounds for free, as a
   byproduct of normal work, not extra admin.
5. **Evaluation, not an afterthought** — a small held-out set of realistic fault
   descriptions with known "correct" categories and precedents, scored on:
   - retrieval precision (did it find the right precedent, if one existed)
   - hallucination rate (did it ever suggest a fix the retrieved evidence didn't
     actually support)
   - honest abstention rate (did it correctly say "no precedent" when there wasn't
     one, instead of guessing)

## 3. Why this, specifically

- It solves a real, expensive problem (institutional knowledge loss), not an
  invented one.
- It takes Klipboard's own stated principle — "a confidently wrong answer is worse
  than no answer" — and builds it into the mechanism itself (confidence gating and
  explicit abstention), rather than bolting it on after the fact.
- It's a genuine RAG/embeddings use case, applied where it earns its complexity
  rather than for its own sake.
- Markdown records serve two purposes at once: human-readable documentation for the
  service adviser, and machine-retrievable memory for the system. Same artifact,
  no duplicated effort.
- It has a natural data moat: the more jobs the garage runs, the better it gets,
  with zero extra curation work.

## 4. Tech stack — what's built vs. what's deliberately deferred

A tool like this could justify a large stack. Given the brief's own framing — "the
exercise is intended to take approximately three hours," and unfinished parts are
fine provided the trade-offs are explained — the stack was scoped deliberately in
two tiers, rather than maximised for its own sake. Building everything below the
line would have diluted focus away from the part of this exercise that actually
matters: the quality and honesty of the AI triage and retrieval itself.

### Core build

- **Runtime**: .NET 10 (current LTS, supported through November 2028), ASP.NET
  Core Minimal APIs.
- **AI abstraction**: Microsoft.Extensions.AI as the model-agnostic layer, backed
  by Google's Gemini API (via the official Google.GenAI package), so the provider
  is a config choice, not a rewrite — matches the JD's "choose pragmatically on
  quality, cost and latency rather than habit."
- **Structured output**: JSON-schema-constrained extraction for the triage record,
  to keep output machine-usable and reduce free-form drift.
- **Memory store**: flat Markdown files (one per resolved job) plus a lightweight
  local embedding index for similarity search — no external DB required for a
  prototype, but the interface is designed so a real vector store could swap in
  later.
- **Consistent `ApiResponse<T>` envelope**: every endpoint returns the same
  success/error/metadata shape, so any future client (including a real frontend)
  can consume it predictably.
- **FluentValidation**: on the intake input, since a malformed or empty fault
  description shouldn't silently reach the model.
- **Serilog**: structured logging around each pipeline stage — directly answers
  the JD's "logging and monitoring" requirement, and is what you'd actually reach
  for first when a triage result looks wrong in production.
- **Rate limiting** (built-in ASP.NET Core middleware): the one infra concern that
  earns its place immediately, since it directly protects against runaway LLM
  token cost and latency — not decorative, genuinely load-bearing for this kind
  of feature.
- **Scalar** for interactive API docs — current, lightweight, and a better default
  than Swagger UI for a new project in 2026.
- **Layered Clean Architecture** as four real projects — `Domain` (zero
  dependencies), `Application` (interfaces, DTOs, validators — depends only on
  Domain), `Infrastructure` (implements Application's ports, owns the AI
  package dependencies), `Api` (thin presentation layer, wires DI). The
  dependency rule is enforced by the project references themselves, not just
  convention — `Domain` and `Application` cannot accidentally take a
  dependency on `Infrastructure` or `Api`, because there's no project
  reference path for the compiler to allow it.
- **`.slnx` solution file** (the current .NET 10 default XML-based format,
  replacing the old GUID-laden `.sln`) tying all five projects together.
- **A base entity** (`Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`) for
  the resolved-job record, so audit trail is there from the first record onward.
- **Evaluation harness**: a small script that runs the held-out test set through
  the pipeline and reports precision, hallucination rate, and abstention
  correctness — built alongside the feature, not after it.
- **One Dockerfile** for the API, so the whole thing runs identically anywhere,
  including a real deploy target if one's wanted later.

### Path to production (documented, not built — and why)

These are real, considered choices for where this goes *if Klipboard wanted to run
it for real* — deliberately left as documentation rather than code, so the 3-hour
budget stays spent on the AI quality problem rather than infrastructure ceremony:

- **CQRS + MediatR** — earns its place once there are enough distinct
  commands/queries (and cross-cutting concerns like validation-as-pipeline-
  behaviour) that the indirection pays for itself; premature here with ~3
  operations. The layered project structure keeps this easy to introduce
  later without a rewrite, if the operation count grows.
- **JWT vs. a full external identity provider — worth distinguishing.** JWT
  issuance and validation is built: the triage endpoint requires a valid
  token, and tokens are properly signed and verified, not decorative. What's
  deferred is the identity provider behind it — right now, a single shared
  secret stands in for a real per-adviser credential store, which is an
  honest simplification for a reviewer to run in minutes, not a production
  posture. A real deployment would swap the credential check in
  `AuthEndpoints.cs` for delegation to Entra ID, Auth0, or similar, while the
  JWT validation on the API side barely changes.
- **Base repository abstraction** — worth it once there's a real database behind
  the Markdown store; premature while the store is flat files.
- **AutoMapper**, **Scrutor** — genuinely useful once the object graph and DI
  registrations grow past what's easy to see by eye; not yet needed at this size.
- **Blazor + MudBlazor frontend, with charts, and Refit as the typed client** —
  the natural next step so an adviser has a real screen instead of an API call,
  and charts would genuinely help visualise retrieval confidence and historical
  fault trends per vehicle system; scoped out here so the exercise stays focused
  on the AI pipeline the role is actually hiring for.
- **Docker Compose across both apps, deployed to Render for both frontend and
  API** — the natural production target once there's a frontend to ship
  alongside the API.

## 5. Scope for the 3-hour exercise

**In scope:**
- Fault intake → structured triage record.
- A small seed set (10–15 handcrafted) of past "resolved job" Markdown records to
  search against.
- Retrieval + confidence-gated suggestion, with explicit abstention when no good
  match exists.
- The write-back step that appends a newly resolved job to the store.
- A minimal evaluation script against a handful of test cases.

**Deliberately out of scope** (and why):
- A real UI — a CLI or a bare API is enough to demonstrate the mechanism; polish
  can come later.
- Multi-vehicle-model nuance in retrieval (e.g. year/trim-specific matching) —
  correct precedent-finding, not exhaustive precedent-finding, is what's being
  proven here.
- Production concerns like auth, multi-tenant storage, or a real vector database —
  noted as "next steps," not built.

## 6. What would make this worth Klipboard actually building

This isn't just a take-home gimmick — the same shape applies directly to
Klipboard's existing garage management product: fault intake already happens
there, technician close-out notes already exist, and the missing piece is just
connecting the two through retrieval instead of losing that knowledge every time
a job is closed out.
