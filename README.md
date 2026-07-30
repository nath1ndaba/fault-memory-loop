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

🚧 Structural skeleton only. This commit is the layered Clean Architecture
(Domain / Application / Infrastructure / Api) with a single health-check
endpoint, proving the pipeline runs — logging, rate limiting, API docs, all
wired. No AI and no authentication yet; both land in their own commits on
top of this.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Setup

1. Clone the repo.
2. Run the API:
   ```
   dotnet run --project src/FaultMemoryLoop.Api
   ```
3. Open the interactive API docs at the URL printed on startup (served via
   Scalar), or hit `GET /health` directly to confirm it's running.

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
