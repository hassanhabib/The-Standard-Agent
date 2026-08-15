# The Standard Agent — Enterprise Roadmap

> This roadmap owns the **why** — the pillars, the seams, and the appliance guarantee. The
> release-by-release execution — which branch, which commit, which conformance vector, and how we
> know it worked — lives in [**enterprise-plan.md**](enterprise-plan.md).

From a five-line laptop demo to a multi-billion-dollar enterprise, on **one** agent
definition. The trick is the doctrine's own principle — **collapsible substrate**: the
public API, the loop, and the Tri-Nature never change; scale lives in the **brokers** and
the **deployment**.

## North star

> One agent definition. Five lines on a laptop, the *same* five lines in a bank. Scale lives
> in the brokers and the deployment — never in the public API, never in the loop, never in the
> Tri-Nature.

`new StandardAgent().Brain(...).Skills(...).Tool(...)` is the appliance. It must never grow a
sixth concept to go enterprise.

## The one rule that keeps it an appliance

Every enterprise capability enters through exactly one of two doors — both already exist:

1. **A broker swap** — `file → Redis`, `LLM-guard → rule-guard`, `in-proc → distributed`.
   Same interface, bigger substrate.
2. **An opt-in builder method with a working default** — `.Audit(...)`, `.Budget(...)`,
   `.Jurisdiction(...)`. Absent ⇒ sane default; present ⇒ enterprise control.

No new mental model at any scale. The Tri-Nature is the whole model, tiny to titan.

## Pillars

Each pillar is a **gap → an enterprise need → the broker seam it rides on.**

### 1. The audit spine
The trace becomes an immutable, structured, exportable **decision log** — what a regulator or
an incident review reads. Close the two holes: **narrate failures** (the trace goes dark on
error today) and add **metrics** (timing / tokens / cost per step). Then a second sink:
OpenTelemetry + JSON. *Seam:* trace sinks + `.Audit(sink)`. *Why first:* nothing else is
enterprise-trustworthy until every decision is explainable and telemetered.

### 2. Guardians that pass an auditor
Bring the **Judge to parity with the Gate** — a reason and a real *revise-out* with feedback,
not a bare score. Add **deterministic rule guardians** (the doctrine already allows
"guardian = deterministic rule"; compliance can't be a coin-flip LLM). Add a **guardian ≠ brain**
check (Invariant 6). *Seam:* `IClassifier` / `IVerifier` — rule *or* model, collapsible.

### 3. The security perimeter
**PII / secret redaction + tokenization at the boundary** — data never reaches the brain in
the clear — plus **jurisdiction / RBAC on tools** (Ch 9–10: capability follows location, gated
at the perimeter; least-privilege per tenant). *Seam:* boundary brokers + policy-as-Data.

### 4. Durable lifecycle & human-in-the-loop
Make `AwaitingInput` **resumable**: a session/continuation store so an agent can pause, escalate
to a human or another system, **die**, and a *different* instance rehydrates and continues. Wire
**human approval before irreversible actions** (Invariant 7). This turns "a nice loop" into "an
agent a bank trusts with a wire transfer." *Seam:* `ISessionStore`.

### 5. Data at scale
Finish Data's two stub legs (the doctrine lists them "Open"): **real Memory** (durable,
shareable, tenant-scoped, with forgetting) and **real Knowledge** (embeddings / RAG / vector).
The adapter family (Redis / Postgres) already has the seams — wire real retrieval. *Seam:* the
existing broker adapter packages.

### 6. Work at scale — fan-out & the fractal
**Direction actually fans out** (parallel effectors — today it is one tool, sequential). And
**native specialist routing**: extend `route in` from skills to sub-agents, so an enterprise is a
mesh of simple specialists, each a five-line appliance, composed via Direction / MCP.
*Seam:* AgentTool / MCP + route → delegate.

### 7. Runtime & deployment surface
**Cost / reliability governance** — token / cost budgets, timeouts, retries, circuit breakers
(MaxTurns is the only budget today). And **form factors from one definition**: library →
ASP.NET service → OpenAI-compatible server → scale-out. Write once, deploy at any scale.
*Seam:* hosting adapters + options.

## Recommended sequence

```
1 Audit spine ──▶ 2 Guardians ──▶ 3 Perimeter        (the compliance trio — first)
        │
        ▼
4 Durable lifecycle ──▶ 5 Data at scale ──▶ 6 Fan-out / fractal ──▶ 7 Deployment
```

The compliance trio (1–3) comes first — enterprise trust is a prerequisite, not a feature, and
each depends on the audit spine. Pillar 4 is the headline capability; 5–7 are scale-of-work and
scale-of-fleet. A regulated target may pull pillar 3 earlier.

## The appliance guarantee (never violate)

- The public builder API is frozen. New capability = a broker swap or an opt-in method with a
  default.
- The Tri-Nature (Data / Decision / Direction) is the only mental model at every scale.
- Brokers are the single variability point. Policy is Data, never code.
- Zero config to start; progressive disclosure of knobs only when needed.
