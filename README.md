# The Standard for Agents

The C# reference implementation of **[The Standard for Agents](https://github.com/hassanhabib/The-Standard-Agent-Specs)**.

```
Agent = Orchestration(Data, Decision, Direction)
```

- **Data** — what the agent *has* (skills, memory, knowledge) · verb: **Recall**
- **Decision** — what the agent *thinks* (one brain, wrapped in a Gate and a Judge) · verb: **Think**
- **Direction** — what the agent *does* (act internally, act externally, or return) · verb: **Act**

Orchestration is not a fourth nature. It is the composition operator — the loop.

## The 1·3·9

| Tier | Count | Members |
|---|---|---|
| **Coordination** | 1 | `AgentCoordinationService` — the only loop: Recall → Think → Act |
| **Orchestration** | 3 | Data · Decision · Direction |
| **Foundation** | 9 | Skills, Memory, Knowledge / Gate, Brain, Judge / Internal, External, Return |
| **Broker** | 8+1 | one liaison per resource, plus the logging support broker |

Nine foundations, eight brokers: `ReturnService` has no broker. It is the dead end — the terminal
Direction hands the result back and touches nothing.

Flow is forward only. A tier never calls the tier above it.

## Governance

Two rulebooks, and they compose:

- **[SPEC.md](https://github.com/hassanhabib/The-Standard-Agent-Specs/blob/main/SPEC.md)** owns
  **contracts and behavior**. It is normative and language-neutral.
- **[The Standard](https://github.com/hassanhabib/The-Standard)** owns **structure, exceptions,
  and process** — brokers, foundations, orchestrations, the `Xeption` model, and FAIL/PASS TDD.

They do not collide: SPEC.md §1 states that *"conformance is about contracts and behavior, not file
layout or language idiom."*

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
conformance/                      language-neutral behavioral vectors
```

## Conformance

Agent behavior involves an LLM and is non-deterministic, so it cannot be asserted directly.
[`conformance/`](conformance/CONFORMANCE.md) instead pins the **deterministic** contracts — the loop,
reply interpretation, tool routing, and the feed-back of results into Data — by scripting the Brain.

```bash
dotnet test                                        # unit tests
dotnet run --project Standard.Agents.Conformance   # spec certification; exit 0 = conformant
```

A **Core** implementation is a minimal viable agent. A **Full** implementation adds real memory,
knowledge, guardian gate/judge, and external tools.

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

[The Standard Software License (TSSL) v1.1](LICENSE).
