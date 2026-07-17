# Minimal Agent

The **minimum viable agent** — the smallest complete Tri-Nature agent that demonstrates
all **9 foundations**, **3 orchestrations**, and **1 coordination**, end to end.

**48 files · one type per file · zero NuGet packages · standalone** (it does not
reference `Standard.Agents` — it *is* the pattern, built from nothing).

Written to be typed live. See [`BUILD-ORDER.md`](BUILD-ORDER.md) for the build sequence.

## Run it

Point it at any OpenAI-compatible `/v1/chat/completions` endpoint:

```bash
set AGENT_API_URL=http://localhost:3000/v1/
set AGENT_API_KEY=<your key>
set AGENT_MODEL=<your model>

dotnet run --project samples/MinimalAgent
```

```
Prompt: What is 89347 * 61293 + 4472?
Agent: 5476350143
```

The model alone answers `546797053` — confidently wrong. The Decision reaches for the
`calculator`, the Direction runs it, the result returns to Data, and the next turn
answers correctly. That is the loop earning its keep.

## What's inside

| Tier | Count | |
|---|---|---|
| **Brokers** | 8 | Skill · Memory · Knowledge · Classifier · Generator · Verifier · Tool · Mcp |
| **Foundations** | 9 | Skill · Memory · Knowledge · Gate · Brain · Judge · Internal · External · **Return** (no broker — the dead end) |
| **Orchestrations** | 3 | Data (`Recall`) · Decision (`Think`) · Direction (`Act`) |
| **Coordination** | 1 | the agent — the loop |

**Real:** Skill (reads `Skills/*.md`), Generator (the LLM), Tool (the calculator), Return.
**Honest stubs:** Memory, Knowledge, Classifier, Verifier, Mcp — wired into the flow so
the shape is complete; swap any one for a real resource and nothing above it changes.

The whole agent is these three lines:

```csharp
context = await this.dataOrchestration.RecallAsync(context);
context = await this.decisionOrchestration.ThinkAsync(context);
context = await this.directionOrchestration.ActAsync(context);

if (context.Status != AgentStatus.Working) break;
```

## Then what

[`Standard.Agents`](../../Standard.Agents) — the library in this repo — is the same 13
services with the same names and tiers, grown up: real brokers, and a `StandardAgent`
front door. The shape doesn't change when it scales.
