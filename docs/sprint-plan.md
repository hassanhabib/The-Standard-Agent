# The Standard Agent — Reach Plan

> The enterprise program ([enterprise-roadmap.md](enterprise-roadmap.md) /
> [enterprise-plan.md](enterprise-plan.md)) asked *what must be true for an enterprise to trust
> this agent*, and finished at `1.1.0.0`. The architecture program
> ([architecture-alignment.md](architecture-alignment.md)) put the shape back on The Standard and
> finished at `1.4.0.0`.
>
> This plan asks a different question: **what stands between "architecturally the best thing in
> this space" and "the thing people actually reach for."**

---

## 1 · The finding

An audit of the framework against what an enterprise-grade agent framework needs found the
perimeter, the failure semantics, the profiles, the spec and the architecture enforcement to be
strong — and the gaps to sit at the two ends: **the beginner's on-ramp** and **the ecosystem
around the core.**

The core is close to complete. What is missing is mostly *packages*, and one real hole in the
client contract.

## 2 · The ranked backlog

Ranked by adoption impact per unit of work. The repo column is why this matters for sequencing —
**most of the top of this list cannot be built in this repository.**

| # | Item | Nature / tier | Repo | Size |
|---|---|---|---|---|
| 1 | **Vector knowledge package** — embeddings, hybrid search, chunking | Data · broker | package | M |
| 2 | **Structured output** — a typed, validated answer | Decision · new foundation | **core** | M |
| 3 | **OpenTelemetry package** — `Turn → Step → Process` as spans | utility · broker | package | S |
| 4 | **Sub-agent boundary** — status flattening, no streaming through | Direction · `AgentTool` | **core** | M |
| 5 | **Hosting package** — `AddStandardAgent()`, principal, cancellation, SSE | exposer | package | S |
| 6 | **Provider packages** — Azure OpenAI, Anthropic, Bedrock | Decision · broker | packages | S each |
| 7 | **Evaluation harness** — golden sets, behavioural diff per skillset version | new project | **core repo** | M |
| 8 | **Response caching** — a decorating broker | broker | **core** | S |
| 9 | **Model escalation** — cheap first, escalate on Judge rejection | Decision | **core** | S–M |
| 10 | **Per-tenant quota** — bound a tenant, not just a prompt | *scope undecided* | — | — |

**Struck from the original list: skills authoring tooling.** The PeerLLM registry and
`Standard.Agents.Data.Skills.PeerLLM` already cover authoring, versioning, sharing and
distribution. What remains splits two ways and neither is a new item:

- **Publish-time conflict linting** belongs in the portal. The framework already detects
  contradictory skills through the Gate's `DetectConflictAsync`, but at *runtime* — you find out
  two skills conflict when a customer hits it. The same rubric run across a skillset at publish is
  a portal feature with the machinery already built, and no prompt registry does it.
- **Behavioural diff** is item 7, not a separate thing.

Those two combine into the reason item 7 is worth more than its rank suggests: **versioned
skillsets plus a golden set answers "did v4 of this skillset make the agent worse?"** The registry
supplies the versioning; the harness supplies the judgement.

**Deliberately not on this list**, for the same reason triggers are not in the framework: rate
limiting (the gateway's), encryption at rest (the store's), tenant isolation enforcement (the
broker you supply), parallel tool execution (one act per turn is a perimeter decision, not a
performance oversight).

## 3 · Sprint 1 — what this repository can actually build

Items 1, 3, 5 and 6 are separate packages and separate repositories. That leaves the core work, in
this order:

| Step | Item | Why this order |
|---|---|---|
| 1 | **Structured output** (#2) | The only real gap in the client contract, and the one every integrator hits |
| 2 | **Sub-agent boundary** (#4) | Multi-agent is the enterprise destination and the boundary loses information |
| 3 | **Response caching** (#8) | Small, fits the decorating-broker pattern exactly, real money |
| 4 | **Model escalation** (#9) | The Judge already produces the signal; cheapest quality/cost win there is |

Item 7 is a new project in this repo and is a sprint of its own.

## 4 · Structured output — the design

**The gap.** `ProcessPromptAsync` returns `string`. That is the entire client contract, so every
consumer wiring an agent into a workflow writes a parser — which is exactly what the framework
talked them out of doing everywhere else. Native tool calling gives structured input *to tools*;
the agent's own answer is always prose.

**Where it belongs.** A schema check asks *is this answer the right shape*, immediately beside the
Judge's *is this answer good enough*. Both are verdicts on a draft before it becomes an answer, at
the same moment, so it is **Decision, Guardian region** — a third guardian of a different kind.

That keeps every count honest:

| | before | after |
|---|---|---|
| Guardian orchestration | Gate, Judge (2) | Gate, Judge, Contract (3) ✔ |
| Decision foundations | Brain, Usage, Gate, Judge (4) | + Contract (5) |
| Foundations total | 14 | 15 |

**The broker is the validator.** A JSON Schema validator is a real external resource, which is what
makes this a foundation rather than loose logic — and it answers the triad without straining:

- **Local** — a minimal in-box validator (types, required, enum). Keeps the core dependency-free.
- **External** — `.UseContract(broker)` for a full JSON Schema library.
- **Custom** — `.OnContract(delegate)` for your own validation.

**The schema travels as a JSON string**, following `ToolDefinition.ParametersJson`, which already
sets that precedent. A typed `T` overload with schema generation can follow in a package; it needs
reflection and does not belong in a dependency-free core.

**A mismatch is a re-think, not a fault.** The Judge revision loop already models exactly this: a
rejection sets `AgentStatus.Revising` and the loop re-asks within the turn budget, and when it
still cannot pass, it refuses gracefully rather than throwing. A schema failure takes the same
path, with the validation error fed back as the revision reason so the model knows what to fix.

## 5 · Definition of done

The bar this repository already holds itself to, restated so nothing slips:

- **FAIL/PASS TDD**, and the FAIL commit is a test *run and observed failing*. Where a test is
  written after the code, it is **sabotage-verified** instead and the commit says so.
- **Brokers carry no unit tests.** They are thin liaisons.
- **The triad is complete** — Local, External, Custom — or the capability matrix test fails the
  build.
- **The tier rules hold**: one nature broker per foundation, nothing above the foundation tier
  takes a broker, two-to-three of the tier directly below.
- **A conformance vector**, proven able to fail, for anything another language must reproduce.
- **The spec says it** if an implementer would otherwise guess. Three gaps this year were silences,
  not wrong statements.
- Zero warnings, all four profiles certified.
