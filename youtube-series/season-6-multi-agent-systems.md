# Season 6 — Multi-Agent Systems

7 episodes · 12–18 min each · the destination the whole series has been walking toward

Season 3.6 showed that an agent satisfies `ITool`, so an agent can be a tool of another agent. That
was one episode and a party trick. This season is the engineering.

**The governing fact, and it decides every episode here:** `RunManagementService` calls
`AgentRun.Begin` at the start of every run. A nested agent is asked through `RunAsync`, which
**begins its own run.** So almost nothing propagates — not the budget, not the identity, not the
run-once scope, not the trace correlation. Each of the seven episodes is a consequence of that
one sentence.

Two things do cross, and both are deliberate. **The stop crosses in:** cancelling the outer run
stops the whole tree at each agent's next turn boundary. **Honesty crosses back:** a sub-agent
that was held, refused, or ran out of turns returns marked `[did not complete]` with its status,
never as prose that reads like an answer.

That is not a defect. A sub-agent with its own guardians and its own turn cap is exactly what makes
it a *distinct conscience* rather than a subroutine. But it means a multi-agent system is a
**distributed system**, and every enterprise control you learned in seasons 4 and 5 has to be
established per agent, deliberately.

---

## 6.1 — Decomposition: when one agent should be several

**Runtime** 13 min · **Branch** `series/s6e1-decomposition`

**Cold open**
> "Your agent has nineteen tools and a four-thousand-word skill file. It is not one agent any more."

**Beats**
- The symptoms that mean you have outgrown one agent: too many tools for the model to choose well,
  a skill file nobody can review, guardrails that need to differ by task, and a turn budget that
  keeps rising.
- The 2–3 rule, applied to *agents* rather than services — and say plainly that this is an analogy
  the framework does not enforce, unlike the tier rules in season 8. It is a heuristic that has
  earned its place, not a checked invariant.
- Split by **concern**, not by step. A "research agent" and a "writing agent" is a real split; a
  "step 1 agent" and a "step 2 agent" is a workflow wearing a costume, and you should build a
  workflow.
- The cost of splitting, stated before the benefit: more model calls, more latency, more surface,
  harder tracing. Decomposition is not free and is frequently premature.
- The honest default: **one agent until it hurts, then split on the seam that hurts.**

**The gotcha**
Every sub-agent is a full agent — its own turns, its own guardians, its own budget. Costs compound
**multiplicatively**. An outer agent with 7 turns calling an inner agent with 7 turns is up to 49
inner turns, each of which may itself be three model calls. Do that arithmetic on camera; it changes
how people design.

---

## 6.2 — The handoff: what one agent tells another

**Runtime** 15 min · **Branch** `series/s6e2-handoff` · **Docs** how-to §17

**Cold open**
> "Passing the user's raw prompt to your sub-agent is the multi-agent equivalent of a global
> variable."

**Beats**
- `AgentTool` in full — it is richer than 3.6 let on:
  ```csharp
  var researcher = new AgentTool(
      name: "researcher",
      agent: innerAgent,
      handoff: "Research this and return three sourced bullet points: {input}",
      description: "Finds and cites background material. Use before drafting.",
      parameters: "{ \"type\": \"object\", \"properties\": { \"topic\": { \"type\": \"string\" } } }");
  ```
- **The handoff template** is the contract between agents. Two slots: `{input}` is the task the
  outer model wrote, `{prompt}` is what the user originally asked. Everything around them is the
  brief, and a template with neither shares nothing at all. Left at its default an `AgentTool` is
  exactly `{input}`, so the raw input passes straight through — the behaviour of 3.6, and rarely
  what you want past the demo. The fleet's default is `AgentTool.GroundedHandoff` — *"The user
  asked: {prompt} — Your task: {input}"* — the task plus just enough context to do it (6.3).
- **The description is the routing logic.** The outer model chooses this agent over another by
  reading it. Write it as *what it does and when to use it*, and remember 2.2's rule: no
  description, not advertised.
- **The parameters schema** is what makes the sub-agent addressable on the native protocol (5.3).
- `AgentTool` takes an **`IAgent`**, not a `StandardAgent` — two required members,
  `RunAsync(PromptRequest, CancellationToken)` and `StreamPromptAsync(PromptRequest,
  CancellationToken)`; every other door (`ProcessPromptAsync`, `RunStreamAsync`, the string
  overloads) has a default built on those two. That is a smaller surface than it looks and it is
  the season's most useful extension point: **anything that satisfies `IAgent` can be nested**,
  including a facade over an agent running in another process, another language, or behind an HTTP
  call. Implement one on camera in ten lines and nest a remote agent — the outer agent cannot tell
  the difference.
- Design the return contract deliberately: the outer agent receives a **string**. Decide its shape —
  bullet points, JSON, a one-line verdict — and put that in the handoff.

**The gotcha**
The outer agent still receives a string, so a sub-agent's *status* can only come back as text. It
used to come back unmarked: an inner agent held on an approval returned a *sentence about
waiting*, which reads exactly like an answer, and an outer agent could report held work as done.
Now anything but an answer comes back as `[did not complete] the sub-agent 'researcher' ended
AwaitingApproval: …` — marked, with the status and the sub-agent's own words. Show both halves: the
marker makes the failure unmistakable, and it is **still not a status the outer run can act on**.
Keep approval-bearing acts in the outer agent. This is the sharpest edge in the season — do not
skip it.

---

## 6.3 — Topologies: supervisor, pipeline, panel

**Runtime** 17 min · **Branch** `series/s6e3-topologies` · **Docs** how-to §17

**Cold open**
> "There are three shapes that work, and about nine that people try first."

**The fleet — the capability that registers other agents (+3 min, all three modes)**
- **Local** `.Agents("Fleet")` — a folder where every `.json` file *is* an agent, the same
  document `FromJson` composes (7.12). Its `"name"` is what a handoff calls, its `"description"`
  advertises it.
- **External** `.UseAgents(IAgentRegistryBroker)` — a provider's registry.
- **Custom** `.OnAgents(() => …)` — your code decides the fleet, as `RegisteredAgent`s.

Registries accumulate, and the first to claim a name keeps it — the MCP rule (2.3). **A registered
agent materializes as a tool**, and that one decision is the whole design: a handoff is an act, so
`AllowTools` can forbid one, `RequireApproval` can put a human before one, and audit, telemetry and
cancellation already apply. Nothing new was needed to govern it.

**Beats**
- **Supervisor / worker.** One outer agent with several sub-agents as tools — `ACTION: billing:
  refund order 7741`. It routes, and synthesizes the answer. This is the default, it composes with
  everything in seasons 4–5, and it is what the fleet is built for.
- **Transfer.** The outer brain recognizes the *whole* prompt belongs to a specialist —
  `TRANSFER: billing` — and the specialist's answer **is** the run's answer, verbatim. No synthesis
  turn, no rewriting. A transfer that does not deliver (refused, held, out of turns) comes back
  `[did not complete]` as an observation and the outer brain keeps working: a refusal is never
  presented as the user's answer. Show both on camera.
- **Pipeline.** Agent A's output is the handoff to agent B. Cheap, predictable, and the shape most
  often better served by a *workflow* calling two agents than by nesting them — say so. That's the
  fleet's **chain**: your code calling agents in order, where determinism belongs; every link keeps
  its own guardians, budget and perimeter.
- **Panel / adversarial.** Several sub-agents answer the same question with different skills or
  different models, and the outer agent reconciles. This is 3.6's independent conscience,
  generalised — and it is the shape that genuinely buys quality rather than just structure.
- Build the supervisor live: a triage agent over a refunds agent and a research agent.
- **Depth versus breadth.** Breadth (many siblings) costs linearly and traces cleanly. Depth
  (nesting through layers) costs multiplicatively and traces terribly. Prefer breadth; cap depth at
  two, and say why on camera.
- Anti-patterns, named: agents that call each other in a cycle, an agent whose only job is to
  forward, and a "manager" agent with no tools of its own that merely re-asks the same model.

**The gotcha**
The fractal needs no new machinery because the shapes already agree — but "no new machinery" is not
"no new failure modes". A cycle between two agents will exhaust turns and budgets rather than
deadlock, which is *better* than hanging and still an outage. Bound every agent (6.4).

---

## 6.4 — Budgets, identity, and approval across agents

**Runtime** 16 min · **Branch** `series/s6e4-propagation`

**Cold open**
> "You set a budget on the outer agent. Watch the inner one ignore it completely."

**Beats**
- Demonstrate the propagation gap first, on camera, with the trace open. The outer `.Budget()`
  bounds the outer loop; the inner agent has its own or none at all.
- The same is true of **identity**: `.Principal()` is per agent. An inner agent with no principal
  makes authorization decisions about nobody — which, per 4.1, the framework refuses to fake for
  you.
- And of **run-once**: the inner run has its own `RunId`, so the idempotency scope differs. An
  effect claimed by the inner agent is claimed under the inner run.
- The one control that *does* cross: **cancellation.** The outer run's token rides the ambient run
  the way its identity does, so cancelling the outer run stops every nested run at its next turn
  boundary. Show a nested tool that used to keep executing after the outer run was cancelled — and
  now executes once.
- The discipline that follows: **configure every sub-agent as deliberately as the outer one.**
  Build a small factory that stamps budget, principal resolver, ledger and audit onto every agent in
  the system, so the controls cannot be forgotten one at a time.
  ```csharp
  StandardAgent Governed(StandardAgent agent) => agent
      .Principal(() => currentUser.Id)
      .OnPolicy(Authorize)
      .EffectLedger("ledger")
      .Audit("audit.jsonl")
      .Budget(maxCostUsd: 0.05m, costPerThousandTokens: 0.002m);
  ```
- Budget the **system**, not the agent: divide a total across the tree, and give the outer agent the
  smaller share because it multiplies.
- Where to put irreversible acts: **in exactly one agent**, as close to the perimeter as possible.
  Scattering them across a tree is how a system loses track of what it can do.

**The gotcha**
This is the episode a large enterprise will judge the whole system on. A multi-agent deployment
where identity does not reach the inner agents has the 1.0 defect from 4.1, at system scale:
identity-aware *reporting*, not identity-aware *authorization*. Say that explicitly, and show the
audit log proving each agent's acts are attributed to the real principal.

---

## 6.5 — Tracing a system, not an agent

**Runtime** 14 min · **Branch** `series/s6e5-tracing`

**Cold open**
> "Something went wrong. It went wrong in one of five agents. Which one?"

**Beats**
- Each agent's run has its own id, so a naive multi-agent trace is five unrelated transcripts.
- The correlation strategy: one audit sink for the whole system, plus a system-level correlation id
  carried in the handoff so inner records can be tied to the outer act. Show the sink receiving
  from every agent.
- Reading a nested trace: the outer agent's `Turn → Step → Process` shows one Tool step, and the
  inner agent's entire run sits underneath it. Point at the boundary on screen.
- What to alert on in a multi-agent system, which is different from a single one: depth exceeded,
  fan-out count, per-agent budget exhaustion, and a sub-agent refusing repeatedly.
- Cost attribution per agent, using `AgentUsage` (4.7) — including `IsEstimated`, because in a
  hybrid system some agents report and some are counted, and a finance conversation needs to know
  which is which.

**The gotcha**
`.Audit` per agent with different paths gives you five files and no system view. One sink, or a
custom `.OnAudit` that stamps the agent name and the correlation id, is the difference between an
incident you can reconstruct and one you can only apologise for.

---

## 6.6 — Failure, compensation, and partial success

**Runtime** 17 min · **Branch** `series/s6e6-failure`

**Cold open**
> "Agent three succeeded. Agent four failed. Agent three's effect is real and nobody is going to
> undo it."

**Beats**
- `.CompensateOnFailure()` unwinds **within a run** (5.2). A sub-agent's run is a different run, so
  its effects are outside the outer agent's unwind.
- Demonstrate the hole deliberately: outer calls inner, inner performs a real effect, outer fails
  afterwards, inner's effect stands.
- The three ways to handle it, with the trade stated for each:
  1. **Keep effects in one agent.** Simplest and usually right. Sub-agents advise; the outer acts.
  2. **Compensate at the boundary.** Give the `AgentTool` a compensating counterpart, so undoing the
     sub-agent's work is itself a declared tool.
  3. **Saga at the system level.** The outer agent owns an explicit sequence with explicit undo, and
     you have left agent territory for workflow territory — which is sometimes the honest answer.
- Partial success as a first-class outcome: report which agents succeeded and which stand, never a
  single boolean.
- What a sub-agent failure looks like to the outer agent: a string — but a **marked** one. The
  framework prefixes every non-answer with `[did not complete]` and the sub-agent's status (6.2),
  so a failure is never mistaken for a successful answer that happens to mention a problem. Teach
  the outer skill what the marker means, and design the rest of the failure contract in the
  handoff: what the outer agent should do next, not just that something went wrong.

**The gotcha**
The recommendation is deliberately conservative: **advise widely, act narrowly.** Fan out for
research, judgement and review; keep every irreversible act in one agent with a ledger, a principal
and an approval. Multi-agent systems that spread effects across the tree are the ones that produce
incidents nobody can reconstruct.

---

## 6.7 — Capstone: a multi-agent system serving a regulated enterprise

**Runtime** ~25 min · **Branch** `series/s6e7-capstone`

**Cold open**
> "Five agents. One customer. Real money. Everything from the last six seasons, at system scale."

**Beats**
- The system, designed with 0.2's worksheet:
  - **Triage** (supervisor) — routes, holds the budget for the tree, owns nothing irreversible.
  - **Research** — knowledge and retrieval, read-only, cheap model.
  - **Compliance** — a distinct conscience with its own constitution and a *different* model.
  - **Drafting** — generation, gated and judged.
  - **Settlement** — the only agent with an irreversible tool, an effect ledger, approval, and the
    strictest policy.
- Build it with the `Governed(...)` factory from 6.4 so no agent can be under-configured, and
  register the four specialists on Triage as a fleet (6.3) — so every handoff passes Triage's
  perimeter, and Settlement's is one `RequireApproval` away from a human.
- Then show the same fleet **as data**: four agent documents in a `Fleet/` folder, `"agents":
  ["Fleet"]` in Triage's document, composed by `FromJson` (7.12). Same system, no C# for the
  topology.
- One audit sink, one correlation id, cost attributed per agent.
- Then break it, at system scale:
  1. Research returns an injected instruction → screened at the boundary.
  2. Compliance refuses → the supervisor routes to a human rather than retrying.
  3. Settlement reaches an irreversible act → approval held, granted out-of-band, resumed.
  4. Kill the process mid-settlement → resumed, replayed, not repeated.
  5. The tree exhausts its system budget → reported distinguishably, per agent.
- Reconstruct the entire multi-agent decision from the audit log alone.
- Certify each agent: `--profile Critical` for Settlement, `Enterprise` for Compliance, `Reliable`
  for Research. **Different agents legitimately hold different profiles**, and saying so is more
  honest than certifying everything at the highest level.

**The gotcha**
Count the model calls at the end and put the number on screen next to the single-agent version from
5.7. That number is the price of the architecture, and a viewer deciding whether to build this
deserves to see it rather than infer it.

**Season close** — "That is a multi-agent system a bank could run. Next: what it takes to keep it
running — and then, for anyone who wants to take the framework apart, the architecture itself."
