# Minimal Agent — Live Build Order

The live-coding spine for building **the minimum viable agent**: all **9 foundations**,
**3 orchestrations**, and **1 coordination**, end to end, with one simple tool.

**48 files. One type per file. Zero NuGet packages.**

Build **bottom-up** — each tier compiles as soon as it's typed, because it only depends
on the tier below it. Nothing is left broken while the camera is rolling.

---

## Before you hit record

```bash
set AGENT_API_URL=http://localhost:3000/v1/
set AGENT_API_KEY=<your key>
set AGENT_MODEL=<your model>
```

Any OpenAI-compatible `/v1/chat/completions` endpoint works.

---

## Step 1 — Models (2 files)

`Models/AgentStatus.cs` · `Models/AgentContext.cs`

> "Everything the agent has is **Data**. `AgentContext` is that Data in flight — one
> record that threads the whole loop. And notice `AgentStatus` is an **enum, not a
> bool** — because the *reason* it stopped matters: it responded, it refused, it's
> waiting on a human."

Point at the three regions of the record: Data / Decision / Direction.

## Step 2 — The two contracts (2 files)

`IAgent.cs` · `Tools/ITool.cs`

> "An agent takes a prompt and returns an answer. A tool takes an input and returns an
> output. **Same shape** — which is why later an agent can *be* a tool. That's the
> fractal."

## Step 3 — The tool (1 file)

`Tools/CalculatorTool.cs`

> "One real tool. Watch what it does to the model later."

## Step 4 — Brokers (16 files)

The thin liaisons. **One resource, one broker. No business logic, no authored prompts.**

- `Brokers/Data/` — `ISkillBroker`/`SkillBroker` (reads `*.md` off disk — **real**),
  `IMemoryBroker`/`MemoryBroker` (stub), `IKnowledgeBroker`/`KnowledgeBroker` (stub)
- `Brokers/Decision/` — `IClassifierBroker`/`ClassifierBroker` (stub: allows),
  `IGeneratorBroker`/`GeneratorBroker` (**real** — the LLM),
  `IVerifierBroker`/`VerifierBroker` (stub: 1.0)
- `Brokers/Direction/` — `IToolBroker`/`ToolBroker` (the registry),
  `IMcpBroker`/`McpBroker` (stub)

> "Five of these are honest stubs. That's the point of *minimum viable* — the shape is
> complete, the resources aren't. Swap a stub for SQLite and nothing above it changes."

## Step 5 — Foundations · **the 9** (18 files)

`Services/Foundations/Data/` — **Skill · Memory · Knowledge**
`Services/Foundations/Decision/` — **Gate · Brain · Judge**
`Services/Foundations/Direction/` — **Internal · External · Return**

> "Nine foundations. Each sits on exactly **one** broker and speaks business language —
> the broker says `Select`, the service says `Retrieve`. And look at `ReturnService`:
> **no broker at all.** It's the dead end — its resource is the caller."

This is the tier to linger on. Nine files, all the same shape.

## Step 6 — Orchestrations · **the 3** (6 files)

`Services/Orchestrations/Data` → **Recall** — what it *has*
`Services/Orchestrations/Decision` → **Think** — what it *decides*
`Services/Orchestrations/Direction` → **Act** — what it *does*

> "Each orchestration coordinates its own three foundations. Decision is the interesting
> one: **Gate** screens the input, the **Brain** thinks, the **Judge** screens the
> output — one brain, wrapped in a conscience."

## Step 7 — Coordination · **the 1** (2 files)

`Services/Coordinations/AgentCoordinationService.cs`

> "This is the agent. Read it out loud:"

```csharp
context = await this.dataOrchestration.RecallAsync(context);
context = await this.decisionOrchestration.ThinkAsync(context);
context = await this.directionOrchestration.ActAsync(context);

if (context.Status != AgentStatus.Working) break;
```

> "**Recall. Think. Act.** Until it's done. That's the whole agent. Everything else was
> plumbing to make these three lines true."

## Step 8 — The client (1 file)

`Program.cs`

> "Eight brokers. Nine foundations. Three orchestrations. One coordination. In that
> order, top to bottom — the architecture *is* the file."

---

## The payoff — run it

```
Prompt: What's the tallest building in the world?
Agent: The tallest building in the world is the Burj Khalifa…      <- one turn, no tool
```

Then the moment:

```
Prompt: What is 89347 * 61293 + 4472?
Agent: 5476350143
```

> "Ask the model that directly and it says **546797053** — confidently wrong. Here the
> Decision chose `ACTION: calculator`, the Direction ran it, the result went **back into
> Data**, and the next turn answered with the truth: **5476350143**. That's why the loop
> exists. That's why Direction exists."

---

## The reveal

> "This is 48 files you just watched me type. `Standard.Agents` — the library in this
> repo — is *the same 13 services*, same names, same tiers. The only difference is that
> its stubs are real and it has a `StandardAgent` front door. **The shape didn't change
> when it grew.** That's the fractal: hand-writable in one sitting, and it scales."

---

## Timing

| Step | Files | ~min |
|---|---|---|
| 1–3 Models, contracts, tool | 5 | 8 |
| 4 Brokers | 16 | 12 |
| 5 Foundations | 18 | 12 |
| 6 Orchestrations | 6 | 10 |
| 7 Coordination | 2 | 5 |
| 8 Client + run | 1 | 8 |
| **Total** | **48** | **~55** |
