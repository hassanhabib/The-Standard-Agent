<div align="center">

<img src="https://raw.githubusercontent.com/hassanhabib/The-Standard-Agent/main/assets/the-standard-agent-icon.png" alt="The Standard for Agents" width="180" />

# The Standard AI Agent Framework

**`Agent = Orchestration(Data, Decision, Direction)`**

The C# reference implementation of **[The Standard for Agents](https://github.com/hassanhabib/The-Standard-Agent-Specs)**.

[![.NET](https://github.com/hassanhabib/The-Standard-Agent/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hassanhabib/The-Standard-Agent/actions/workflows/dotnet.yml)
[![Nuget](https://img.shields.io/nuget/v/Standard.Agents?logo=nuget&style=default&color=blue)](https://www.nuget.org/packages/Standard.Agents)
![Nuget](https://img.shields.io/nuget/dt/Standard.Agents?color=blue&label=Downloads)
[![The Standard - COMPLIANT](https://img.shields.io/badge/The_Standard-COMPLIANT-2ea44f?style=default)](https://github.com/hassanhabib/The-Standard)
[![The Standard](https://img.shields.io/github/v/release/hassanhabib/The-Standard?filter=v2.50.0&style=default&label=Standard%20Version&color=2ea44f)](https://github.com/hassanhabib/The-Standard)
[![The Standard Community](https://img.shields.io/discord/934130100008538142?style=default&color=%237289da&label=The%20Standard%20Community&logo=Discord)](https://discord.gg/vdPZ7hS52X)
[![License: TSSL](https://img.shields.io/badge/license-TSSL%20v1.1-blue.svg)](https://github.com/hassanhabib/The-Standard-Agent/blob/main/LICENSE.txt)

</div>

---

The mark is the thing itself: three arcs — **Data**, **Decision**, **Direction** — orbiting a
single core. One brain at the center, the three natures turning around it.

- **Data** — what the agent *has* (skills, memory, knowledge) · verb: **Recall**
- **Decision** — what the agent *thinks* (one brain, wrapped in a Gate and a Judge) · verb: **Think**
- **Direction** — what the agent *does* (act internally, act externally, or return) · verb: **Act**

Orchestration is not a fourth nature. It is the composition operator — the loop.

## Install

```bash
dotnet add package Standard.Agents
```

```csharp
var agent = new StandardAgent()
    .Skills("Skills")
    .Brain(apiUrl: "https://api.peerllm.com/v1/", apiKey: key, model: "LLooMA2.0")
    .Tool(new CalculatorTool())
    .LogTo("log.txt");

string answer = await agent.ProcessPromptAsync("What is 47 * 89?");
```

No DI container. `Compose()` hand-wires the whole graph — SPEC.md §9: *"DI is OPTIONAL. A
hand-wired composition root is fully conformant."*

## The 1·3·9

| Tier | Count | Members |
|---|---|---|
| **Coordination** | 1 | `AgentCoordinationService` — the only loop: Recall → Think → Act |
| **Orchestration** | 3 | Data · Decision · Direction |
| **Foundation** | 9 | Skills, Memory, Knowledge / Gate, Brain, Judge / Internal, External, Return |
| **Broker** | 8+2 | one liaison per resource, plus logging |

Nine foundations, eight nature brokers: `ReturnService` has no broker. It is the dead end — the
terminal Direction hands the result back and touches nothing.

Flow is forward only. A tier never calls the tier above it.

## Governance

Two rulebooks, and they compose:

- **[SPEC.md](https://github.com/hassanhabib/The-Standard-Agent-Specs/blob/main/SPEC.md)** owns
  **contracts and behavior**. Normative, language-neutral.
- **[The Standard](https://github.com/hassanhabib/The-Standard)** owns **structure, exceptions,
  and process** — brokers, foundations, orchestrations, the `Xeption` model, FAIL/PASS TDD.

They do not collide: SPEC.md §1 states that *"conformance is about contracts and behavior, not
file layout or language idiom."*

The theory is settled in
**[THE-TRI-NATURE-OF-AGENT.md](https://github.com/hassanhabib/The-Standard-Agent-Specs/blob/main/THE-TRI-NATURE-OF-AGENT.md)**
before any code is written. Build to it.

## Structure

```
Standard.Agents/                  the library
  |-- Brokers/{Data,Decision,Direction,Loggings}
  |-- Models/Foundations/{Entity}/Exceptions
  |-- Models/Orchestrations/Agents          AgentContext, AgentStatus
  |-- Services/{Foundations,Orchestrations,Coordinations}
  |-- Tools/                                ITool, AgentTool — the fractal bridge
Standard.Agents.Tests.Unit/       unit tests, mirroring the service tree
Standard.Agents.Conformance/      the vector runner
Standard.Agents.Demo/             a console agent you can run
conformance/                      language-neutral behavioral vectors
```

## Conformance

Agent behavior involves an LLM and is non-deterministic, so it cannot be asserted directly.
[`conformance/`](https://github.com/hassanhabib/The-Standard-Agent/blob/main/conformance/CONFORMANCE.md) instead pins the **deterministic** contracts — the
loop, reply interpretation, tool routing, and the feed-back of results into Data — by scripting
the Brain. Every double replaces a **broker**, never a service: the whole 1·3·9 under test is the
real library.

```bash
dotnet test                                        # unit tests
dotnet run --project Standard.Agents.Conformance   # spec certification; exit 0 = conformant
```

A **Core** implementation is a minimal viable agent. A **Full** implementation adds real memory,
knowledge, guardian gate/judge, and external tools.

## The fractal

An agent satisfies `ITool`, so an agent can be a tool of another agent. Theory Ch.4 — *turtles up*:

```csharp
var researcher = new AgentTool("researcher", innerAgent);
var outerAgent = new StandardAgent().Brain(...).Tool(researcher);
```

Nesting needs no new machinery because the shapes already agree. It is also how a guardian scales:
a compliance sub-agent is a distinct conscience, rather than the same brain grading itself.

## Contributing

This repo follows [The Standard's practices](https://github.com/hassanhabib/The-Standard):

- One issue per method. One branch per issue.
- Branch: `users/[username]/[CATEGORY]-[entity]-[action]`, where the action speaks the language of
  its layer — brokers `insert`/`select`, foundations `add`/`retrieve`.
- Foundations and up are test-driven, two commits per test:
  `[TestName] -> FAIL`, then `[TestName] -> PASS`. A FAIL commit must have been *run and observed
  failing*.
- Brokers carry no unit tests — they are thin and hold no logic. Commit as `BROKERS: [Description]`.
- PR title: `[CATEGORY]: [Description Of Work Completed]`.

## License

[The Standard Software License (TSSL) v1.1](https://github.com/hassanhabib/The-Standard-Agent/blob/main/LICENSE.txt).
