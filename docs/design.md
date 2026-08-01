# Fault Memory Loop
### A self-improving diagnostic memory for garage fault intake
*Design brief — Klipboard AI Software Developer Take-Home Exercise*

## A note on scope, read this first

This went well beyond the brief's "approximately three hours." What exists
now is a five-project Clean Architecture solution with dual authentication
(Google OAuth2/OIDC + email/password against a real database), live Gemini
integration, a working retrieval loop that demonstrably gets smarter over
time, and a real evaluation harness that caught two genuine bugs during
development.

That's worth being direct about rather than glossing over. The honest
reason: this became as much a hands-on exploration of what the role and
the problem could look like as literal compliance with a time box, and
that was a deliberate choice, not scope creep I failed to notice.

If I'd stopped at the stated scope, the three-hour version would have been:
this design doc, the intake → structured `TriageRecord` step only, a
handful of hardcoded knowledge-store examples with no real retrieval
scoring, and no auth at all (since a reviewer needs to run a take-home in
minutes, not configure an identity provider). Everything past that point —
the layered architecture, both login paths, real retrieval, and the eval
harness — was chosen to build, not the exercise strictly asking for it.

The judgment call this section is making explicit: knowing when to stop is
exactly what the role's JD asks for, and going past the stated scope here
is a real, opposite risk worth naming rather than hiding.

## Tech stack at a glance

**Built:** .NET 10 · layered Clean Architecture as four real projects
(`Domain` / `Application` / `Infrastructure` / `Api`, `.slnx` solution,
dependency direction enforced by project references, not convention) ·
ASP.NET Core Minimal APIs · Microsoft.Extensions.AI + Gemini (model
configurable via `GEMINI_MODEL`, not hardcoded) · Serilog · FluentValidation
· rate limiting · Scalar · JWT issuance and validation · Google OAuth2/OIDC
verification (real `GoogleJsonWebSignature` check, not a stand-in) ·
email/password login against a real `Employee` table (EF Core + SQLite,
PBKDF2 password hashing) · a real Markdown-backed knowledge-store
repository · tag-overlap retrieval with honest abstention · a real
evaluation harness scoring retrieval precision, abstention correctness, and
hallucination rate against held-out test cases, run against the actual
pipeline (not mocked) · consistent `ApiResponse<T>` envelope · base entity
audit fields · Dockerfile.

**Deliberately deferred, documented as a production roadmap instead:**
embeddings/semantic similarity for retrieval (see the honest limitation
note in Section 4) · CQRS + MediatR · a generic repository base
abstraction · AutoMapper · Scrutor · Blazor + MudBlazor frontend + Refit ·
Docker Compose + Render deployment for both apps.

## 0. Picture the counter

A customer walks into the shop. His car has been making a noise. He is not
a mechanic — he says something like:

> "There's a clicking sound when I turn the wheel, and it feels like the
> car pulls to the left a bit. Started a few days ago, gets worse the
> sharper I turn."

The service adviser at the counter has to turn that into something useful
in the next sixty seconds. If the adviser is new, or hasn't seen this
fault type before, they're guessing — or waiting for a senior tech to
become free for a two-minute conversation that decides everything.

Now imagine that same shop has quietly fixed a near-identical fault before.
That knowledge exists — it's just locked inside whichever technician
happened to diagnose it. The adviser at the counter has no way to reach it.

This is the moment the tool is built for: the adviser types what the
customer just said, and gets back either "here's what this looked like
last time, here's what actually fixed it, here's how confident we are" —
or an honest "we haven't seen this exact pattern before."

## 1. The problem

When a fault comes in as free text, an experienced technician often
recognises the pattern instantly. That recognition is tribal knowledge —
undocumented, held in one person's head, lost when that person is
unavailable or moves on. Most fault-intake tools stop at classification.
This one is designed to make the garage's own historical knowledge
reusable, and to get better every time a job closes.

## 2. The idea

**Fault Memory Loop**: an intake tool that triages incoming fault
descriptions *and* consults a growing, evidence-backed memory of the
garage's own past resolved jobs before offering any suggestion — closing
the loop automatically once a job is confirmed fixed.

### Flow, as actually built

1. **Intake** — `POST /api/triage`, authenticated. The adviser submits the
   customer's raw words plus whatever vehicle context is on file. Gemini
   (via `Microsoft.Extensions.AI`'s `IChatClient` abstraction) extracts a
   structured `TriageRecord`: likely system, fault category, urgency,
   symptom tags, and clarifying questions — never a diagnosis guess.
2. **Recall** — before returning anything, `TagOverlapRetrievalService`
   searches confirmed past resolved jobs in the knowledge store, scoring
   by symptom-tag overlap gated to the same vehicle system. If a strong
   match exists (similarity ≥ 0.5), it's surfaced with a citation to the
   specific job. If not, the response says so explicitly — this is what
   the eval harness's abstention-correctness metric checks.
3. **Close the loop** — `POST /api/jobs/resolve`, authenticated. Once a
   technician confirms the actual diagnosis and fix, it's written as a new
   Markdown file in `knowledge-store/jobs/`, complete with the system and
   symptom tags needed for future retrieval to actually find it. Verified
   end to end during development: a fault that initially found no
   precedent matched its own resolution on resubmission.
4. **Evaluation** — `src/FaultMemoryLoop.Eval` runs the real pipeline
   (same `GeminiTriageExtractionService` and `TagOverlapRetrievalService`
   the API uses) against `eval/test-cases/cases.json`, scoring:
   - retrieval precision — did it find the right precedent when one existed
   - abstention correctness — did it correctly say "no precedent" when
     there wasn't one
   - hallucination rate — did any suggestion claim a fix not backed by a
     cited job (structurally difficult here, since recommendations are
     built directly from a cited job's `ActualFix`, never generated freely)

## 3. Why this, specifically

- Solves a real, expensive problem (institutional knowledge loss), not an
  invented one.
- Builds Klipboard's own stated principle — "a confidently wrong answer is
  worse than no answer" — into the mechanism itself (confidence gating,
  explicit abstention, `OutcomeConfirmed` gating on retrieval), not bolted
  on after.
- Markdown records serve two purposes at once: human-readable
  documentation and machine-retrievable memory, in the same file.
- Real evaluation caught real bugs during development — not a token
  gesture, an actual working feedback loop that changed the system
  (see Section 4's honest limitation note for a concrete example).

## 4. Architecture and honest limitations

### What's built vs. deferred

Domain / Application / Infrastructure / Api as four real projects, with
dependency direction enforced by project references — Domain has zero
dependencies, Application depends only on Domain, Infrastructure implements
Application's ports, Api wires DI. Two independent login paths (Google
OAuth2/OIDC, email/password against a real database) both issue the same
kind of JWT. Real Gemini extraction and real Markdown-backed retrieval.

Deferred, with reasons: CQRS + MediatR (premature at ~4 operations), a
generic repository base abstraction (premature with one real database
entity), AutoMapper/Scrutor (premature at this object-graph size), a
Blazor frontend (out of scope for what this role is actually hiring for —
the AI pipeline, not frontend work), production deployment tooling
(Docker Compose + Render for both apps — a real next step, not built).

### Honest limitation, with real evidence

Retrieval uses Jaccard tag overlap on `symptomTags`, not semantic
embeddings. During evaluation, this surfaced a genuine weakness: Gemini's
`symptomTags` extraction isn't perfectly deterministic between calls on
the same input, so a case that should match a known precedent can
occasionally score just below the similarity threshold, even though a
human would clearly recognise it as the same fault. This is a real,
observed limitation, not a hypothetical one — and it's the concrete
argument for the semantic-embeddings upgrade path: embedding-based
similarity would be far more robust to this kind of surface-level wording
variance than exact tag matching is.

## 5. What would make this worth Klipboard actually building

The same shape applies directly to Klipboard's existing garage management
product: fault intake already happens there, technician close-out notes
already exist, and the missing piece is connecting the two through
retrieval instead of losing that knowledge every time a job is closed out.
