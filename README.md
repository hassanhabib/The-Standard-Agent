# The Standard for Agents

**The Standard for building AI agents according to the Tri-Nature Theory.**

An agent is not an LLM, a tool, a prompt, or a memory. An agent is the **orchestration
of three natures**:

```
Agent = Orchestration(Data, Decision, Direction)
```

- **Data** — what the agent *has* (skills, memory, knowledge)
- **Decision** — what the agent *thinks* (one brain, wrapped in a Gate and a Judge — the
  agent's own conscience)
- **Direction** — what the agent *does* (act internally, act externally, or return)

---

## Three pillars

| | What it is | Answers |
|---|---|---|
| [`THE-TRI-NATURE-OF-AGENT.md`](THE-TRI-NATURE-OF-AGENT.md) | **The theory** — the model, the fractal, lifecycle & memory, safety, governance | *why* |
| [`SPEC.md`](SPEC.md) | **The specification** — a language-agnostic, RFC-2119 contract with Core & Full conformance profiles | *what to build, in any language* |
| [`Standard.Agents/`](Standard.Agents) | **The reference implementation** — a Standard-compliant C# library (.NET 10, zero external dependencies) | *one proof* |

The theory says *why*, the spec says *what you must build*, and the C# is *one* way to
build it. A conformant JavaScript, Go, Rust, or Python version proves itself against the
same spec.

---

## Architecture — the 1·3·9 tiers

`Coordination → Orchestration → Foundation → Broker`, flow forward only.

![Architecture](architecture.svg)

- **Coordination (1)** — the Agent. The only tier that loops: `Recall → Think → Act`,
  until a terminal status.
- **Orchestration (3)** — Data (`Recall`), Decision (`Think`), Direction (`Act`).
- **Foundation (9)** — Skills · Memory · Knowledge · Gate · Brain · Judge · Internal ·
  External · Return.
- **Broker (8+1)** — one thin liaison per resource; `Return` is the dead end (no broker).

---

## Quick start (C# reference implementation)

Requires the **.NET 10 SDK** and an **OpenAI-compatible** chat endpoint (for example a
local [PeerLLM](https://peerllm.com) host, or any `/v1/chat/completions` server).

1. Set your endpoint in `Standard.Agents.Demo/appsettings.json` — fill in `ApiUrl`,
   `ApiKey`, and `Model` (the committed `ApiKey` is intentionally blank).
2. Run the demo:
   ```bash
   dotnet run --project Standard.Agents.Demo
   ```
3. Try a prompt:
   ```
   What is 89347 * 61293 + 4472?
   ```
   The agent recalls its skills, decides to use the calculator tool, feeds the result
   back into Data, and returns the answer — the full loop through every tier.

---

## Build it in your own language

[`SPEC.md`](SPEC.md) is the normative, language-neutral blueprint. It defines every
contract in neutral pseudo-types, the loop as a normative algorithm, the reply protocol,
and nine MUST/MUST-NOT invariants — using RFC-2119 keywords so "compliant" is testable.

Two conformance profiles:
- **Core** — a minimal viable agent (Skill + Brain + Internal tool + Return).
- **Full** — adds real Memory & Knowledge, guardian Gate & Judge (distinct from the
  Brain), and External (MCP).

---

## Status

The reference implementation runs the full 1·3·9 tier end to end.

- **Real:** `SkillService`, `BrainService` (OpenAI-compatible generator), `InternalToolService`,
  `ReturnService`, and the flow-log support broker.
- **Honest stubs** (wired into the flow, ready to fill): `MemoryService`, `KnowledgeService`,
  `GateService`, `JudgeService`, `ExternalToolService`.

---

## Key invariants

- Everything is Data; prompts, rules, and rubrics live in Data (skills), never in code.
- One agent, one brain. Many brains is the fractal — a higher-order agent delegating to
  sub-agents.
- The agent instance is ephemeral; persistent memory lives outside it.
- A guardian is never the Brain — a faculty cannot certify its own trustworthiness.
- Irreversible actions are authorized before execution, never after.
- Security rests on the perimeter (jurisdiction), never on the visitor's conscience.
