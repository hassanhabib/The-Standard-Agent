# The Standard for Agents
## Book I — The Tri-Nature of Agent

> An agent is not an LLM, a tool, a prompt, or a memory.
> An agent is the **orchestration of three natures**.

```
Agent = Orchestration(Data, Decision, Direction)
```

- **Data** is what the agent **has**.
- **Decision** is what the agent **thinks**.
- **Direction** is what the agent **does**.
- **Orchestration** is what **binds** them into agency.

This document is the living source of the Standard. The theory is settled here first;
code is built to it.

---

## Chapter 1 — The Three Natures

Everything in an agent falls into exactly one of three buckets.

### Data — what it HAS  ·  verb: Recall
Any static information: configuration, skills, identity, instructions, rules, context,
retrieved content, prompts, and responses. **Once content is produced, it becomes Data.**
Data does not think and does not act. It is only material to be interpreted.

### Decision — what it THINKS  ·  verb: Think
The **act** of reasoning under ambiguity. Not data in itself — the *process* of making
decisions that lead to actions. Deterministic evaluation is not Decision (that is
computation). Another mind's judgment is not this agent's Decision (that is external).
Decision is only the agent's *own* reasoning.

### Direction — what it DOES  ·  verb: Act
Any action: calling an API or MCP server, running a tool, persisting back to Data,
returning the response, asking a question, refusing an unsafe action.

### Orchestration — what BINDS them
Orchestration is **not a fourth nature**. It is the composition operator — the loop.
Every concrete thing it does reduces to one of the three natures.

---

## Chapter 2 — The Loop

```
new context
   ↓
Recall (Data) → Think (Decision) → Act (Direction)
   ↑____________________________________|
        (Direction sends its result back into Data)
   ↓
repeat until a terminal Status
```

One `AgentContext` — **Data in flight** — threads the loop. Each nature reads it and
returns an updated copy. The loop ends at a terminal Status.

---

## Chapter 3 — The Carriers

**Every model is Data.** The nature is the *act*, never the struct. `AgentContext` is the
one record that threads the loop; its regions are marked by which nature last wrote them.

```csharp
public sealed record AgentContext
{
    public string Prompt { get; init; } = "";

    // DATA — what it HAS
    public string SystemPrompt { get; init; } = "";
    public IReadOnlyList<string> Observations { get; init; } = [];

    // DECISION — what it THINKS
    public string Intent { get; init; } = "";
    public string DirectionType { get; init; } = "";
    public string Payload { get; init; } = "";
    public string RawReply { get; init; } = "";

    // DIRECTION — what it DID
    public string Result { get; init; } = "";
    public AgentStatus Status { get; init; }
}
```

**`AgentStatus` is an enum, never a boolean** — the terminal states *are* the terminal
Directions, and new behaviors slot in without touching the loop:

```csharp
public enum AgentStatus { Working, Responded, AwaitingInput, Refused, Failed }
```

---

## Chapter 4 — The Fractal Property

The pattern repeats **upward and downward**. Same shape at every scale.

- **Upward** — a Direction can be another whole Agent (an agent wrapped as a tool).
  Agents nest inside agents. Turtles up.
- **Downward** — each nature decomposes again, *where genuine physical multiplicity
  exists*. Turtles down. (The fractal is descriptive, not a mandate: do not force three
  where the world has one.)

```
Agent
├── Data      = (Skills, Memory, Knowledge)   — three sources
├── Decision  = (Gate,   Brain,  Judge)       — one brain, flanked by conscience
└── Direction = (Internal, External, Return)  — three loci
```

---

## Chapter 5 — The Sub-Natures

Each nature's legs are organized along that nature's natural dimension. The whole reads
as one sentence: *what I was taught, remember, and can look up → screened, thought, and
verified → acted out, in, or not at all.*

### Data = (Skills, Memory, Knowledge) — by SOURCE (fan-in)
| Leg | Is | Broker |
|---|---|---|
| **Skills** | authored — what I was taught (identity, instructions, tools, behavior) | `SkillBroker` (files) |
| **Memory** | experienced — what I remember (persistent, shareable, across sessions) | `MemoryBroker` (durable store) |
| **Knowledge** | retrieved — what I can look up now (RAG, search, documents, live data) | `KnowledgeBroker` (index/API) |

### Decision = (Gate, Brain, Judge) — ONE brain, wrapped in conscience
An agent has **one brain**. Multiplicity of brains is not an intra-agent feature — it is
the **fractal** (a higher-order agent choosing which specialist sub-agent to consult, via
Direction). The Brain is flanked by two conscience faculties that are **not brains**:

| Leg | Role | May be |
|---|---|---|
| **Gate** (Classifier) | screen the INPUT — accept / refuse / route in | deterministic rule · LLM guardrail · human |
| **Brain** (Generator) | the one reasoning engine — *thinks the task* | the LLM |
| **Judge** | screen the OUTPUT — accept / refuse / revise out | deterministic rule · LLM verifier · human |

Physically, the three are *interfaces* distinguished by output shape — a **label**
(classify), **text** (generate), a **score** (judge). At small scale one model wears all
three hats; at enterprise scale they separate into specialized models. **Three interfaces,
collapsible substrate.** For anything safety-critical the guardian must **not** be the
brain (see Chapters 8–10).

### Direction = (Internal, External, Return) — by LOCUS (fan-out)
| Leg | Is | Example |
|---|---|---|
| **Internal** | act within the agent's own environment | a local/in-process tool, a memory write |
| **External** | act outward, across the boundary | MCP server, remote API, another agent |
| **Return** | no action — hand the result back | the terminal Direction; the response to the caller |

### The Hourglass
Data **fans in** (many sources → one context); Direction **fans out** (one decision →
many effectors); the Brain is the singular waist. Data and Direction are plural because
they **touch the world**; the Brain is singular because it touches **only its own mind**.

---

## Chapter 6 — The Boundary Principle

> Everything *outside* the agent reaches it only through **Direction** (going out) and
> **Data** (coming back). The three natures are strictly the agent's *interior*.

It explains every hard case:
- A rule's *content* → **Data** (Skills). Its deterministic evaluation → **Direction**;
  the verdict → **Data**.
- A human → **Direction** (ask) → their answer → **Data**.
- Another agent's brain → **Direction** (delegate via MCP / AgentTool) → its reply → **Data**.
- The agent's *own* inference → **Decision** — the only thing that never crosses the boundary.

**Configuration is not a nature.** It is system composition — how the brokers *function*.
Infrastructure config → the Brokers. Behavioral preferences → fold into Skills.

---

## Chapter 7 — Lifecycle & Memory

**The agent instance is ephemeral compute. Memory is a durable, external resource.**

- The instance spins up, **recalls, thinks, acts, persists — and dies.** It carries no state
  between prompts.
- **Working memory** (within one prompt) lives in the context and dies with the loop.
- **Persistent memory** lives *outside* the flow — a durable, shareable store behind a
  broker. **Recalled by Data, written by Direction.**
- The gap between prompt N and N+1 is irrelevant to the agent: it always recalls from the
  store. If a day passes, the instance is gone; the store remains.

---

## Chapter 8 — Judgment: Reflection vs Guardianship

Judgment splits by one question: **can the same brain be trusted to judge this?**

| Kind | Trust | Mechanism |
|---|---|---|
| **Reflective** (quality, grounding, coherence) | the brain *wants* to get it right | **the loop** — the draft becomes Data, the next Think reconsiders it (same brain) |
| **Guardian** (policy, safety, refusal) | the brain may *want* to violate | a **distinct conscience** — never the same brain: a deterministic rule, a separate verifier, or a compliance *sub-agent* (the fractal) |

**You cannot let a faculty certify its own trustworthiness.** A heretic (uncensored) brain
judging its own output will approve it. The refusal must come from a mind that is *not* the
brain. Reflection is *in-band*; guardianship is *out-of-band*.

Corollary: **producing a draft ≠ committing to return it.** The brain's output is a *draft*
(internal Data). Only a deliberate, post-judgment Direction returns it. Nothing crosses the
boundary un-vetted.

---

## Chapter 9 — Safety: The Privilege Boundary

> The **Decision→Direction boundary is a privilege boundary** — like userspace → kernel.
> The brain is untrusted userspace: it may *propose* anything. Direction is the privileged
> syscall, where proposals become real-world effects. A heretic brain with no guardian is
> **running as root with no permission checks.**

The dividing line for *where* the guardian sits is **reversibility**:

| | Guardian position | Why |
|---|---|---|
| **Reversible** output (a draft) | may loop — stash → judge → return | nothing left the boundary; retract for free |
| **Irreversible** action (cut power, move money, delete, deploy) | **must gate BEFORE execution — never after** | you cannot un-ring the bell |

For catastrophic actions the guardian is **layered by stakes** — and never a single
fool-able model:
1. **Deterministic rules** — hard blocks that cannot be talked out of (Rules-as-Data,
   evaluated deterministically — un-jailbreakable).
2. **Human approval** — `AwaitingInput` for the irreversible / high-stakes.
3. **An LLM verifier** — for nuance, but never the sole guard on anything catastrophic.

Enforce on both sides for depth: Decision won't *commit* an unauthorized action, and
Direction won't *execute* one. The guardian is not optional for any agent with real
Direction capability — it is the authorization layer that makes an agent safe to give
hands at all.

---

## Chapter 10 — Governance: Jurisdiction

**An environment is a jurisdiction** — it plays the tri-nature at a higher order to govern
the agents inside it (its Data = the laws; its Decision = "is this permitted here?"; its
Direction = grant / revoke / quarantine / expel).

When an agent enters an environment, two things happen — and keeping them separate is the
whole security story:

1. **The place feeds its rules into the agent's Data.** The agent *learns* local law —
   same agent, different environment, different Data, different behavior ("when in Rome").
   This is the *cooperative* path.
2. **The place enforces at its own boundary with its own guardian.** Because **you cannot
   trust a visitor to self-police.** A rogue agent ignores the rulebook you hand it.

> **Self-governance is cooperative and subvertible. Jurisdiction is authoritative and
> final. Security rests on the perimeter, never on the visitor's conscience.**

- **Capability follows location, gated at the perimeter.** Tools are granted by the
  jurisdiction; the environment's guardian ensures the agent cannot reach past its grant.
  More trust → more tools; locked-down place → almost none.
- **Data carries a policy hierarchy, not a flat blob.** Home values < environmental mandate
  < higher jurisdiction; the Judge enforces precedence (local law supersedes home preference
  for actions taken here). Rules-as-Data are inert until a Judge reads and gates on them.

The Judge is therefore not one component in one agent — it is the **enforcement fabric of
the whole cyberspace.** Every boundary between minds and capabilities has one, or that
boundary is not real. Without judges: rough agents gaining access where they should not.
That is the hospital, and the matrix, same root cause.

---

## Chapter 11 — Structural Mapping (to The Standard)

| Concept | Standard element |
|---|---|
| The three natures | **Services** — `DataService.Recall`, `DecisionService.Think`, `DirectionService.Act` (one interface each) |
| Each sub-nature's resource | **Brokers** — thin liaisons, one resource per broker, no flow control, no authored prompts |
| The loop | **Orchestration** — pure composition; holds no nature logic |
| Data in flight | **`AgentContext`** — one record; every model is Data |
| Lifecycle state | **`AgentStatus`** enum |
| An agent as a tool | **`ITool`** — the fractal bridge (a Direction may be a leaf tool or a whole Agent) |

Flow is forward only: `Exposer → Orchestration → Service → Broker → resource`.
Prompts, rules, and rubrics are **Data** (Skills), never authored in code.

---

## Settled Invariants

0. Everything is Data, Decision, or Direction. Orchestration binds; it is not a fourth nature.
1. Every model is Data. Once content is produced, it becomes Data.
2. An agent has one brain. Multiplicity of brains is the fractal (agents of agents).
3. Prompts / rules / rubrics live in Data (Skills), never in code.
4. The agent is ephemeral; persistent memory lives outside it, recalled by Data, written by Direction.
5. Producing a draft ≠ committing to return it. Nothing crosses a boundary un-vetted.
6. You cannot let a faculty certify its own trustworthiness. Guardians are never the brain.
7. Irreversible actions are authorized before execution — by rule and/or human, never after.
8. Security rests on the perimeter (jurisdiction), never on the visitor's conscience.

## Open / To Settle
- Whether Gate/Brain/Judge get explicit separate interfaces now or later.
- Knowledge (retrieval) and Memory brokers are theory until wired.

---

*The Standard for Agents — Book I. Settled before code. Build to it.*
