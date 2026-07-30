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

🚧 Work in progress — built incrementally, commit by commit, as part of an AI
Software Developer take-home exercise.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Gemini API key ([Google AI Studio](https://aistudio.google.com/apikey))

## Setup

1. Clone the repo.
2. Copy `.env.example` to `.env` and add your own Gemini API key:
   ```
   GEMINI_API_KEY=your-key-here
   ```
   `.env` is git-ignored — never commit a real key.
3. Run the API:
   ```
   dotnet run --project src/FaultMemoryLoop.Api
   ```
4. Open the interactive API docs at the URL printed on startup (served via
   Scalar).

## Running the evaluation harness

```
dotnet run --project src/FaultMemoryLoop.Eval
```

Reports retrieval precision, hallucination rate, and abstention correctness
against the held-out test set in `eval/test-cases/`.

## Project structure

```
src/
  FaultMemoryLoop.Api/       minimal API, endpoints, DI wiring
  FaultMemoryLoop.Eval/      evaluation harness
docs/
  design.md                 problem framing, scenario, architecture rationale
eval/
  test-cases/                held-out fault descriptions used for scoring
knowledge-store/
  jobs/                      resolved job records (Markdown), grows over time
```

## License

Personal exercise submission — not for redistribution.
