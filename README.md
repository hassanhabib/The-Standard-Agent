![The Standard for Agents](https://raw.githubusercontent.com/hassanhabib/The-Standard-Agent/main/assets/the-standard-agent-cover.png)

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

---

The mark is the thing itself: three arcs — **Data**, **Decision**, **Direction** — orbiting a
single core. One brain at the center, the three natures turning around it.

- **Data** — what the agent *has* (skills, memory, knowledge) · verb: **Recall**
- **Decision** — what the agent *thinks* (one brain, wrapped in a Gate and a Judge) · verb: **Think**
- **Direction** — what the agent *does* (act internally, act externally, or return) · verb: **Act**

Orchestration is not a fourth nature. It is the composition operator — the loop.

Built for everyone who ships agents: the individual who wants the simplest thing that works, the
small team that needs it reliable, and the enterprise that needs it accountable — **the same agent
grows all the way up, never a rewrite.**

## Watch — the YouTube sessions

[![Create Your First AI Agent from Scratch — The Standard for Agents](https://raw.githubusercontent.com/hassanhabib/The-Standard-Agent/main/assets/the-standard-agent-video-thumbnail.jpg)](https://www.youtube.com/watch?v=UE6QcvQsOyU)

The sessions are live and ongoing on
[Hassan Habib's YouTube channel](https://www.youtube.com/c/hassanhabib): start with
[**Create Your First AI Agent from Scratch**](https://www.youtube.com/watch?v=UE6QcvQsOyU)
above, then the
[**The-Standard sessions playlist**](https://www.youtube.com/playlist?list=PLG2w4duP-rS2adJjAs4QvYIuKriacLQBU)
for the engineering discipline every line of this framework is built on — subscribe there for
the sessions on each new capability as it ships.

## Install

```bash
dotnet add package Standard.Agents
```

## One agent, three sizes

The Tri-Nature is the whole model at **every** size. An agent is always **Data · Decision ·
Direction** turning in a loop — so a ten-second one-liner and a bank's compliance agent are the
*same shape*. You never learn a second framework to scale up: you add capability, one opt-in line at
a time, to natures that are already there. **Enterprise isn't harder — it's the same agent with more
power and more detail.**

### Simple — an individual, one line, ten seconds

You have a brain (a URL, a key, a model). That is a working agent.

```csharp
var agent = new StandardAgent(apiUrl: "https://api.peerllm.com/v1/", apiKey: key, model: "LLooMA2.0");

string answer = await agent.ProcessPromptAsync("What is 47 * 89?");
```

No skills, no tools, no guardians — all opt-in. Nothing to configure. It is already talking.

### Medium — a small team, reliable, a handful of lines

A startup that needs something that *works*: a persona, the ability to act, a conscience on the way
in and out, and a memory that survives restarts. Same builder, a few more lines.

```csharp
var agent = new StandardAgent(url, key, "LLooMA2.0")     // your brain
    .Skills("Skills")                                    // who it is — Markdown, not code
    .Tool(new CalculatorTool())                          // what it can *do*
    .Gate(apiUrl: url, apiKey: key, model: "LLooMA2.0")  // screen the request  (Decision)
    .Judge(apiUrl: url, apiKey: key, model: "LLooMA2.0") // review the answer   (Decision)
    .Memory("memory.txt");                               // remember across runs (Data)
```

### Enterprise — a regulated deployment, more power, same shape

A 20-year professional shipping into a bank. **Every line above is still here — you added, you did
not rewrite.** What's new is power: data that never leaves in the clear, a person between the agent
and anything irreversible, effects that cannot happen twice, and an audit trail a regulator can read.

```csharp
using Standard.Agents.Models.Loggings;   // TraceVerbosity

var agent = new StandardAgent(url, key, "LLooMA2.0")
    .Skills("Skills")
    .Tool(new WireTransferTool())
    .Gate(apiUrl: url, apiKey: key, model: "LLooMA2.0")
    .Judge(apiUrl: url, apiKey: key, model: "LLooMA2.0")
    .Memory("memory.txt")
    .Constitution("Constitution/ethics.md")   // one law above both guardians
    .Redact()                                  // PII tokenized before the brain, restored after
    .LogTo("log.txt", TraceVerbosity.Full)     // full turn-by-turn decision trace
    .Audit("audit.jsonl")                      // structured decision log → your SIEM
    .Telemetry("teller-agent")                 // OTel spans + metrics — GenAI semconv, no packages

    .Principal(() => currentUser.Id)           // authorization has a subject
    .OnPolicy(Authorize)                       // your rules decide, per act and per identity
    .RequireApproval("wire_transfer")          // a person, before the act — never after
    .EffectLedger("ledger")                    // run once, even across a crash
    .ScreenToolOutput()                        // tool results are untrusted input
    .Sessions("sessions")                      // conversation that survives the process
    .Budget(maxCostUsd: 0.25m)                 // bound what one prompt may spend — any protocol
    .Resilience(retries: 3)                    // survive a blip without paying twice
    .CompensateOnFailure();                    // unwind a run that died halfway
```

Everything below the blank line is opt-in, each with a sane default and none of them a new concept.
That is the collapsible substrate: the public API, the loop, and the Tri-Nature never change; power
lives in the brokers and the deployment. Delete any line and the agent still runs, with that
capability absent rather than half-configured.

The full walkthrough — one capability per step, every snippet runnable — is in
[**docs/how-to.md**](https://github.com/hassanhabib/The-Standard-Agent/blob/main/docs/how-to.md).

### The three ways to reach anything

Every capability answers **Local**, **External** and **Custom**, and the verbs say which:

```csharp
.Knowledge("Knowledge")                        // Local    — point at what you already have
.UseKnowledge(new PostgresKnowledgeBroker(cs)) // External — a provider package
.OnKnowledge(async query => await MyStore(query)) // Custom — your own code, inline
```

Nineteen capabilities, the same three verbs each. A capability offered fewer ways than that fails the
build — it is a test, not a convention, because the erosion is otherwise invisible until there are
six of them.

And what the agent integrates *with* is always plural: tools, MCP servers, and skill sources all
**accumulate** — a second `.Mcp(...)` adds a server (each with its own optional auth: none, an
API key, or an OAuth bearer token), a second `.Skills(...)` adds a folder, and calls route to
whichever server's catalog owns the tool's name.

### Agent as data — the whole thing as JSON

There is a fourth door, and no mainstream framework has it: the **entire configurable surface as
one JSON document**, one key per capability, the same names as the builder verbs. Any platform
that can push a form into a JSON body can define an agent — guardians, budgets, perimeter,
approvals, redaction, telemetry and all:

```csharp
var agent = StandardAgent.FromJson(formBody);   // data
    // .Tool(new CalculatorTool())              // code still chains — tools ARE code
```

```json
{
  "brain": { "apiUrl": "https://api.peerllm.com/v1/", "apiKey": "k", "model": "LLooMA2.0" },
  "ruleGate": ["password"], "redact": true, "maxTurns": 5,
  "requireApproval": ["wire_transfer"], "budget": { "maxCostUsd": 0.25 },
  "telemetry": "form-built-agent"
}
```

A key the agent does not know **refuses to compose, with the key named** — a typo'd `"buget"`
must never produce an unbounded agent that looks configured. Tools stay code because they are
code — except MCP, where a tool is a URL, which is data. And the deployment half is one file:
drop an `agent.json` beside `Standard.Agents.Host` and the hosted agent composes entirely from
it — no C# anywhere
([docs/how-to.md §16](https://github.com/hassanhabib/The-Standard-Agent/blob/main/docs/how-to.md)).

## Streaming, and no DI

Stream the agent thinking and answering — each event is tagged, and the answer arrives live:

```csharp
await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync("What is 47 * 89?"))
{
    switch (streamEvent.Type)
    {
        case AgentStreamEventType.Thinking: /* the model deliberating / tool reasoning */ break;
        case AgentStreamEventType.Response: /* the answer, token by token */              break;
        case AgentStreamEventType.Tool:     /* a tool ran, and its result */              break;
        case AgentStreamEventType.Status:   /* lifecycle: turns, gate, judge */           break;
    }
}
```

Streaming is not a lesser path. Budgets, cancellation, sessions, the perimeter and compensation all
hold here exactly as they do on `ProcessPromptAsync` — a control you can step around by changing
method is not a control. Pass a session id to stream inside a conversation:
`agent.StreamPromptAsync(prompt, sessionId, cancellationToken)`.

No DI container. `Compose()` hand-wires the whole graph — SPEC.md §9: *"DI is OPTIONAL. A
hand-wired composition root is fully conformant."*

## Provider packages — swap any nature to a real backend

The core [`Standard.Agents`](https://www.nuget.org/packages/Standard.Agents) is deliberately
**dependency-free**: a hosted brain, and local-file skills, memory, and knowledge. Reach further with
an opt-in package — each one backs a single nature with a real provider through the **same broker
seam**, so it's *one line and nothing else about your agent changes*. Mix them freely: a local brain
with cloud data, a registry of skills with a Redis memory.

| Package | Backs | Provider |
|---|---|---|
| [`Standard.Agents.Decision.Brains.LlamaSharp`](https://www.nuget.org/packages/Standard.Agents.Decision.Brains.LlamaSharp) [![](https://img.shields.io/nuget/v/Standard.Agents.Decision.Brains.LlamaSharp?style=flat-square&label=%20&color=1f6feb&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Standard.Agents.Decision.Brains.LlamaSharp) | Brain · *Decision* | a **local GGUF** model via llama.cpp — no API, no network |
| [`Standard.Agents.Data.Skills.PeerLLM`](https://www.nuget.org/packages/Standard.Agents.Data.Skills.PeerLLM) [![](https://img.shields.io/nuget/v/Standard.Agents.Data.Skills.PeerLLM?style=flat-square&label=%20&color=1f6feb&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Standard.Agents.Data.Skills.PeerLLM) | Skills · *Data* | versioned skills from the **PeerLLM registry**, pulled at runtime |
| [`Standard.Agents.Data.Memory.Redis`](https://www.nuget.org/packages/Standard.Agents.Data.Memory.Redis) [![](https://img.shields.io/nuget/v/Standard.Agents.Data.Memory.Redis?style=flat-square&label=%20&color=1f6feb&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Standard.Agents.Data.Memory.Redis) | Memory · *Data* | memory in **Redis**, keyed per agent / user / session |
| [`Standard.Agents.Data.Knowledge.Postgres`](https://www.nuget.org/packages/Standard.Agents.Data.Knowledge.Postgres) [![](https://img.shields.io/nuget/v/Standard.Agents.Data.Knowledge.Postgres?style=flat-square&label=%20&color=1f6feb&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Standard.Agents.Data.Knowledge.Postgres) | Knowledge · *Data* | knowledge in **PostgreSQL**, ranked `tsvector` full-text |
| [`Standard.Agents.Data.Knowledge.MsSql`](https://www.nuget.org/packages/Standard.Agents.Data.Knowledge.MsSql) [![](https://img.shields.io/nuget/v/Standard.Agents.Data.Knowledge.MsSql?style=flat-square&label=%20&color=1f6feb&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Standard.Agents.Data.Knowledge.MsSql) | Knowledge · *Data* | knowledge in **SQL Server**, `FREETEXT` full-text |

> The **Gate** and **Judge** take your own delegate too — `.OnGate` / `.OnJudge` drive them with any
> in-process model, no package needed.

```csharp
// fully local — one GGUF drives brain, gate and judge, no network anywhere
var llama = new LlamaSharpGeneratorBroker("model.gguf");
var agent = new StandardAgent()
    .UseGenerator(llama)
    .OnGate(llama.GenerateAsync)
    .OnJudge(llama.GenerateAsync);

// production — skills from the registry, knowledge in Postgres, memory in Redis
var agent = new StandardAgent(url, key, "LLooMA2.0")
    .UseSkills(new PeerLLMSkillBroker("hassanhabib/my-skills", SkillSync.Hybrid))
    .UseKnowledgePostgres(pgConnectionString)
    .UseMemoryRedis("localhost:6379", key: $"agent:{userId}");
```

Pick each nature's home independently; the code above doesn't change when you do — that's the whole
promise of the broker seam.

## From the 1·3·9 to the 1·3·6·15

![The Standard for Agents — architecture: StandardAgent → RunManagement → the three nature Coordinations (Data, Decision, Direction) → six Orchestration regions → fifteen Foundation services → their fourteen nature Brokers, with four utility and two decorating brokers beneath](https://raw.githubusercontent.com/hassanhabib/The-Standard-Agent/main/assets/the-standard-agent-architecture.png)

*The diagram is generated from
[`assets/the-standard-agent-architecture.svg`](https://github.com/hassanhabib/The-Standard-Agent/blob/main/assets/the-standard-agent-architecture.svg)
— edit the SVG, re-render the PNG.*

**The 1·3·9 is the core shape of an agent.** One loop, three natures, and beneath them the
foundations — the nine that have been there since the beginning:

| | |
|---|---|
| **Data** | Skill, Knowledge, Memory |
| **Decision** | Brain, Gate, Judge |
| **Direction** | InternalTool, ExternalTool, Return |

`ReturnService` has no broker. It is the dead end — the terminal Direction hands the result back
and touches nothing.

### The 1·3·6·15

Fifteen is what actually ships. Six foundations joined the nine, and the reason each one had to
be a foundation is the same: it is a distinct resource with its own failures, and **a resource
reached without a foundation has no validation, no exception mapping, and its failures get blamed
on the caller.**

| Tier | Count | Members |
|---|---|---|
| **Management** | 1 | `RunManagementService` — the only loop: Recall → Think → Act |
| **Coordination** | 3 | Data · Decision · Direction — the three natures |
| **Orchestration** | 6 | Retrieval, Recollection / Inference, Guardian / Perimeter, Execution |
| **Foundation** | 15 | the nine, plus **Usage** and **Contract**, and Session, Policy, Approval, EffectLedger |
| **Broker** | 14 + 6 | fourteen nature brokers, four utility, two decorating |

**Eleven of the fifteen are always there** — the agent proper, the nine plus `Usage` and
`Contract`. The other four
arrive with the enterprise capabilities; leave them off and it is the same shape with less in it.

Six foundations under Direction alone breaks the 2–3 rule, and a nature holding six is a nature
holding two concepts. So each nature splits into two **regions**, named for concepts rather than
contents: **Retrieval** is what was authored and ranked, **Recollection** is what accumulated;
**Inference** asks the model and reads its answer, **Guardian** is the conscience on either side of
it; **Perimeter** asks *may this happen*, **Execution** does it.

`Usage` is why Decision has two regions rather than one. Measuring what a model call cost is the
same concept as making it — and without it, `.Budget()` could only bound a run whose provider
volunteered its own numbers, which is to say not the text protocol at all.

`Contract` is the third guardian, beside the Gate and the Judge. The Judge asks whether an answer
is good *enough*; the Contract asks whether it is the right *shape* — and a draft that fails its
declared contract is revised the same way one the Judge rejects is, with the validator's complaint
handed back verbatim.

**Every tier holds two or three of the tier directly below it.** Management over coordinations,
coordination over orchestrations, orchestration over foundations, foundation over exactly one
broker. Both bounds cost something when broken: more than three means the service is doing too
much; **fewer than two means it composes nothing and is a layer for its own sake.**

The rest of the placement rule, all of it enforced by
[`TierDisciplineTests`](https://github.com/hassanhabib/The-Standard-Agent/blob/main/Standard.Agents.Tests.Unit/Architecture/TierDisciplineTests.cs)
rather than by convention:

- A **foundation wraps exactly one nature broker.** The role may be versioned —
  `IGeneratorBrokerV1` is the same seam as `IGeneratorBroker` under a newer contract — but it is
  one role. A second broker in a foundation is a capability that wants its own foundation.
- **Nothing above the foundation tier takes a broker at all**, beyond the three utilities.
- **Utility brokers** — logging, time, audit, telemetry — are held by any tier. None of them can
  change what the agent decides *or does*, which is exactly why they are exempt from the count.
  Usage is not one of them: a budget stops a run.
- **Decorating brokers** — redaction, resilience — are wrapped around another broker at
  composition, so no service holds them.

- **Every tier composes the tier directly below it** — management over coordinations, coordination
  over orchestrations, orchestration over foundations. Adjacency is a rule of its own, not a
  by-product of the count: a service reaching *past* the tier below it borrows another nature's
  internals, and no test of counts or brokers will ever see that.

Flow is forward only. A tier never calls the tier above it, no foundation calls a sibling, and
there are no exceptions. Screening a tool result — `.ScreenToolOutput()` — is the **loop** asking
Decision for a verdict, because what may re-enter the context between turns is the loop's question
and the loop is the only place that sees both natures.

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
  |-- Brokers/{Skills,Generators,Tools,...}   one flat folder per seam — twenty in all
  |-- Models/Foundations/{Entity}/Exceptions
  |-- Models/Orchestrations/Agents          AgentContext, AgentStatus, ToolExchange
  |-- Models/Orchestrations/Effects         AgentEffect, AgentPrincipal, CompensationOutcome
  |-- Models/Brokers/Generators/V1          the native tool-calling contract
  |-- Services/Foundations/{Entity}s          fifteen; one broker each, Return none
  |-- Services/Orchestrations/{Nature}/...    six regions, grouped by nature
  |-- Services/Coordinations/{Nature}         Data, Decision, Direction
  |-- Services/Managements                    RunManagementService — the loop
  |-- Tools/                                ITool, AgentTool — the fractal bridge
Standard.Agents.Tests.Unit/       unit tests, mirroring the service tree
Standard.Agents.Conformance/      the vector runner
Standard.Agents.Evals/            the quality runner - golden cases, thresholded metrics
Standard.Agents.Host/             the same agent definition as a web service
Standard.Agents.Demo/             a console agent you can run
conformance/vectors/              language-neutral behavioral vectors
conformance/profiles/             what each readiness level requires
evals/golden/                     golden quality cases the build certifies against
docs/how-to.md                    one capability per step, every snippet runnable
docs/evals.md                     the quality metrics and how to golden-set your own agent
docs/hosting.md                   the agent behind HTTP - runs, streams, heartbeat
docs/support.md                   stability, deprecation, supply chain
docs/generator-contracts.md       text protocol vs native tool calls
```

## Conformance

Agent behavior involves an LLM and is non-deterministic, so it cannot be asserted directly.
[`conformance/`](https://github.com/hassanhabib/The-Standard-Agent/blob/main/conformance/CONFORMANCE.md) instead pins the **deterministic** contracts — the
loop, reply interpretation, tool routing, and the feed-back of results into Data — by scripting
the Brain. Every double replaces a **broker**, never a service: the whole 1·3·6·15 under test is the
real library.

```bash
dotnet test                                        # unit tests
dotnet run --project Standard.Agents.Conformance   # spec certification; exit 0 = conformant
```

Readiness is claimed **per profile**, and the claim is checkable by anyone from a clone:

```bash
dotnet run --project Standard.Agents.Conformance -- --profile Critical
```

| Profile | What an agent at this level has |
|---|---|
| **Core** | conversation, skills, knowledge, memory, simple tools |
| **Reliable** | guardians that see what they guard, durable decision log, run isolation, cancellation, timeouts |
| **Enterprise** | identity-aware authorization, approval before irreversible acts, run-once effects, budgets, ranked retrieval |
| **Critical** | conversation and effects that survive a process, compensation, native tool calls that round-trip, adversarial evaluation — a fooled Brain still cannot act outside the perimeter |

All four certify. Exit `0` means certified; the runner is the authority, not this table.

Beside contract certification sits **quality certification**: golden eval cases measuring task
completion, groundedness, retrieval precision and recall, tool selection, refusal correctness
and revision effectiveness — thresholded, deterministic, and run on every build
([docs/evals.md](https://github.com/hassanhabib/The-Standard-Agent/blob/main/docs/evals.md)):

```bash
dotnet run --project Standard.Agents.Evals
```

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
