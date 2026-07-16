# The Standard for Agents

**The Standard for building AI agents according to the Tri-Nature Theory.**

```
Agent = Orchestration(Data, Decision, Direction)
```

- **Data** — what the agent *has* (skills, memory, knowledge)
- **Decision** — what the agent *thinks* (one brain, wrapped in a Gate and a Judge)
- **Direction** — what the agent *does* (act internally, act externally, or return)

This repository is the **C# reference implementation** (`Standard.Agents`, .NET 10, zero
external dependencies).

> **Theory, specification, and architecture** live in the specs repo:
> **[hassanhabib/The-Standard-Agent-Specs](https://github.com/hassanhabib/The-Standard-Agent-Specs)**
> — the Tri-Nature theory, the language-agnostic RFC-2119 specification, and the
> architecture diagram. Build a conformant framework in any language against those.

## Quick start

Requires **.NET 10** and an **OpenAI-compatible** chat endpoint. Set `ApiUrl`, `ApiKey`,
and `Model` in `Standard.Agents.Demo/appsettings.json` (the committed `ApiKey` is blank),
then:

```bash
dotnet run --project Standard.Agents.Demo
```

## Using the library

The library wires every broker and service under the hood. A consumer provides only
what is theirs:

```csharp
var agent = new StandardAgent()
    .Skills("Skills")              // where the skill files live
    .Brain(apiUrl, apiKey, model)  // the LLM
    .Tool(new CalculatorTool());   // the tools it offers

string answer = await agent.ProcessPromptAsync("What is 89347 * 61293 + 4472?");
```

`StandardAgent` also exposes `UseMemory`, `UseKnowledge`, `UseGate`, `UseJudge`, `UseMcp`,
`UseGenerator`, `UseLog`, and `LogTo` to swap the default brokers for real implementations.

## Architecture — the 1·3·9 tiers

`Coordination → Orchestration → Foundation → Broker`, flow forward only.

- **Coordination (1)** — the Agent; the only tier that loops: `Recall → Think → Act`.
- **Orchestration (3)** — Data, Decision, Direction.
- **Foundation (9)** — Skills · Memory · Knowledge · Gate · Brain · Judge · Internal ·
  External · Return.
- **Broker (8+1)** — one thin liaison per resource; `Return` is the dead end (no broker).

## Status

- **Real:** Skill, Brain (OpenAI-compatible generator), InternalTool, Return, plus the
  flow-log support broker.
- **Stubs** (wired into the flow, ready to fill): Memory, Knowledge, Gate, Judge, External.

## Conformance

`Standard.Agents.Conformance` runs the language-neutral vectors in
[`conformance/vectors`](conformance/vectors) against the reference library:

```bash
dotnet run --project Standard.Agents.Conformance
```

Exit code `0` means every vector passes. See [`conformance/CONFORMANCE.md`](conformance/CONFORMANCE.md).

## License

The Standard Software License (TSSL) — see [LICENSE](LICENSE).
