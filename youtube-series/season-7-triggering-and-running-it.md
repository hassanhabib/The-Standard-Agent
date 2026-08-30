# Part 7 — Triggering It, and Running It

10 episodes · 10–16 min each · no new profile — this is how the agent **starts**, and how you operate it

Every episode until now assumed something called `ProcessPromptAsync`. This part is about what that
something is, and about the fact that nobody ships a console app.

**The framework has no trigger abstraction, and that is correct.** A trigger is the Client/Exposer
tier — outside the agent, the same way Orchestration is not a fourth nature. A cron job, a queue
consumer and an API controller are all exposers.

**But the trigger kind is a design input, not a host detail.** It decides whether a human can
approve in-session, whether identity exists at all, whether a budget is advisory or load-bearing,
and — the one that bites — whether run-once actually protects you.

---

## 7.1 — One instance, many callers

**Runtime** 13 min · **Branch** `series/s7e1-lifetime` · **Docs** support.md → *Thread safety and lifetime*

**Cold open**
> "You built one agent per request. That's why your latency chart looks like that."

**Beats**
- **One `StandardAgent` instance is safe to use concurrently, and is the intended shape.** Register
  it as a **singleton**. Do not build one per request.
- Why it's safe, precisely — this is not "we hope so":
  - Composition is guarded, so concurrent first calls build **one** graph rather than several.
  - Run state — identity, counters, timing, guardian verdicts — is **per invocation, never per
    instance**, so two prompts in flight cannot corrupt each other's records.
  - The trace and decision log serialize their own writes.
- **Verified, not asserted:** 64 concurrent prompts on one instance with both sinks configured, in
  `DecisionLogTests`, plus conformance vector `concurrent-runs-are-isolated`. Run it on camera.
- Show the cost of getting it wrong: per-request construction re-reads skills, re-opens brokers, and
  throws away every cached composition.

**The gotcha — this is the episode**
**Builder methods are not safe to call while prompts are in flight.** Configure the agent, then serve
with it. Calling a builder mid-flight invalidates the cached composition and races the callers
already inside it. The API makes this easy to get wrong because the builder returns `this` and looks
chainable forever — say so plainly.

**What changed in the shape** — nothing. This is the object's lifetime, not its structure.

---

## 7.2 — Triggers: the seven ways an agent starts

**Runtime** 14 min · **Branch** `series/s7e2-triggers`

**Cold open**
> "Everything you've built so far started because you typed into a console. Almost nothing in
> production starts that way."

**Beats**
- The taxonomy, on screen for the whole episode:
  ```
  Triggers
    ├── Human      — a person, waiting
    ├── Schedule   — a fixed time
    ├── Interval   — every N
    ├── Webhook    — someone else's HTTP call
    ├── Event      — a message off a bus
    ├── Agent      — another agent (AgentTool, part 6)
    └── System     — the agent's own machinery: retry, watchdog, sweep
  ```
- **The framework models exactly one of these** — Agent, via `AgentTool`. The other six are yours,
  and that is a deliberate boundary, not an omission: the exposer tier is where a host's world
  belongs, and a framework that shipped a scheduler would be shipping opinions about your
  infrastructure.
- The one line that organises everything else: **is a human present?**
  - **Present** (Human): approval round-trips in the session; the principal is the user; a stuck run
    gets noticed.
  - **Absent** (Schedule, Interval, Webhook, Event, System): approval must be asynchronous, identity
    must be *supplied* rather than assumed, and nobody will notice a runaway loop but the invoice.
- The second question: **is the payload trusted?** Human and Schedule, broadly yes. Webhook and
  Event, absolutely not — and the initial prompt is then untrusted inbound in exactly the sense of
  4.6.
- Map each trigger to the capabilities it makes mandatory rather than optional. Put that table on
  screen; it is the reason this episode exists.

**The gotcha**
Every capability in seasons 4–5 was introduced as opt-in. **The trigger decides which ones stop
being optional.** An interval-triggered agent without a budget is a standing order to spend money;
an event-triggered agent without the trap in 7.5 handled will perform effects twice. Same agent,
same code, different trigger, different minimum configuration.

**What changed in the shape** — nothing inside the agent. Everything about what must be configured.

---

## 7.3 — Human-triggered: web API, chat surface, and the request

**Runtime** 15 min · **Branch** `series/s7e3-http`

**Cold open**
> "Same agent. Now it has to answer forty people at once and never lose a conversation."

**Beats**
- ASP.NET minimal API in front of the part 4 agent. Singleton registration, one endpoint.
- Map `sessionId` to the caller's conversation — the session per user, one agent for everyone.
- Streaming over the wire: `StreamPromptAsync` → SSE, with the four event types preserved so the
  client can render Thinking and Response differently.
- `.Contract(schema)` — the response as a contract, and the per-request form (`ResponseSchemaJson`)
  for a caller that needs a different shape than the agent's default. All three modes here:
  `Contract(schema)` / `UseContract(broker)` / `OnContract(delegate)`.
- `.Principal(() => currentUser.Id)` wired to the **real** authenticated user from `HttpContext` —
  this is where 4.1's "resolved per act, not captured at composition" stops being theory.
- Approval when a human *is* present: the run stops at `AwaitingInput`, the API returns that state,
  the UI asks, the next call resumes the session. Show the round trip.
- Config and secrets from configuration, never source.

**The gotcha**
Per-request state that *looks* like it belongs on the agent — the principal, the session id, the
cancellation token — all travel as **arguments or resolvers**, never as builder calls. That's the
whole reason the agent can be a singleton, and it's why `.Principal` takes a `Func` rather than a
value.

**What changed in the shape** — the agent gained an exposer. Nothing inside it moved.

---

## 7.4 — Unattended: schedule and interval

**Runtime** 15 min · **Branch** `series/s7e4-unattended`

**Cold open**
> "It runs at three in the morning. Nobody approves anything at three in the morning."

**Beats**
- A hosted service with a cron schedule and an interval worker, both calling the same singleton
  agent. Ten lines each; the framework contributes nothing and shouldn't.
- **Identity for an unattended run.** There is no `HttpContext` and no user. The host must supply a
  **service principal** — and per 4.1 the framework will not mint one, because inventing an identity
  would make an authorization decision about a fiction. Show `.Principal(() => serviceIdentity)` and
  a policy that treats a service principal differently from a person.
- **Approval when nobody is watching.** `.RequireApproval` still holds the act, but nothing will
  answer interactively. Wire `.OnApproval` to poll a queue or a ticket, and let the run end in
  `AwaitingInput` — the act is held, the session carries what it is waiting on, and a later run
  resumes it. That's 4.4 and 4.8 doing exactly what they were built for.
- **Budgets stop being advisory.** An interval trigger is a standing order. `.Budget(maxCostUsd:)`
  per run *and* a cap on concurrent runs, or a bad prompt becomes a bad month.
- **Overlapping runs.** If the interval is shorter than the runtime, two runs overlap. The agent is
  thread-safe (7.1), so that is *safe* — but two runs doing the same work is waste at best and a
  double effect at worst. Guard with a lease, or make the schedule skip while one is in flight.
- Cancellation and wall-clock budget as the watchdog: nothing else will stop a stuck unattended run.

**The gotcha**
An unattended agent has no user to be confused by a bad answer — which sounds like lower stakes and
is the opposite. A human-triggered agent has a human reading every output; a scheduled one may run
for weeks before anyone reads a trace. **Audit is not optional on an unattended trigger**, and the
alert you want is "this ran and refused" as much as "this ran and failed."

**What changed in the shape** — nothing. Everything about what must be configured.

---

## 7.5 — Webhook and event: untrusted at the front door

**Runtime** 16 min · **Branch** `series/s7e5-events`

**Cold open**
> "The message that just triggered your agent came from outside your company. So did the
> instructions in it."

**Beats**
- A webhook endpoint and a queue consumer, both invoking the agent with a payload nobody on your
  team wrote.
- **The prompt itself is untrusted inbound.** Season 4.6 screened tool *output*; the same category
  of thing is now arriving as the *initial prompt*. The Gate already screens every prompt (3.1), so
  the control exists — the point of the episode is that people don't realise it applies at the front
  door. Demonstrate an injected instruction arriving as the trigger payload and being refused.
- Never pass a raw payload as the prompt. **Template it**, exactly like the handoff in 6.2: your
  words, their data in a named slot. It doesn't make the data safe, but it stops the payload from
  *being* the instruction.
- Validate and bound the payload before it reaches the agent — size, shape, source. That's ordinary
  API hygiene and the agent is not a substitute for it.

**The gotcha — the sharpest one in this part, and it has a trap inside the trap**

**At-least-once delivery breaks run-once, and the obvious fix does not fix it.**

`AgentEffect.For` derives the key as `DeriveKey(runId, toolName, arguments)` — **the run id is part
of the key.** A redelivered event starts a *new run*, gets a *new run id*, derives a *different key*,
and the effect happens **twice**. The ledger protects against a duplicate *proposal within* a run —
a retry, a model re-proposing — not against a duplicate *trigger*.

The tempting fix is to bind the trigger to the session:

```csharp
await agent.ProcessPromptAsync(prompt, sessionId: $"evt-{eventId}", cancellationToken);
```

**Show this failing on camera.** It rescues one case and not the one you care about. Run continuity
reuses the interrupted run's identity only when the session **did not deliver** — a crash, a
cancellation, a held approval. A run that *completed* and answered is `Responded`, so the next
delivery starts a fresh run with a fresh key and performs the act again. And a lost ack after
successful processing is the single most common at-least-once scenario there is.

Conformance vector `a-repeat-in-a-session-is-a-new-act` pins exactly this, and it expects the tool
to run **twice** — because that is correct. An agent told "send another reminder" in the same
conversation must be able to.

**So the real answer: deduplicate at the trigger boundary.** It is the host's job, for the same
reason identity is (4.1) — delivery semantics are something only the host knows. Keep a processed-id
set, or make the consumer transactional, and drop the redelivery before it reaches the agent.

```csharp
if (await seen.AddAsync(eventId) is false) return;   // already handled; do not invoke
await agent.ProcessPromptAsync(prompt, sessionId: $"evt-{eventId}", cancellationToken);
```

Keep the session binding — it still earns its place for crashes and held approvals. It is just not
the deduplication.

**What changed in the shape** — nothing, and that is the point: the framework will not invent
delivery semantics any more than it will mint a principal. SPEC 1.2 states the boundary so nobody
has to discover it as a duplicate transaction.

---

## 7.6 — Stopping: cancellation and timeouts

**Runtime** 10 min · **Branch** `series/s7e6-cancellation`

**Cold open**
> "The user closed the tab nine seconds ago. Your agent is still spending their money."

**Beats**
- `ProcessPromptAsync(prompt, cancellationToken)` and the `sessionId` overload.
- The run stops at the **next turn boundary** — the smallest unit the loop can stop between without
  leaving an effect half-recorded.
- **Cancellation is never reported as success.** A cancelled run's result is not an answer; it
  arrives as `Status`, distinguishable from a refusal and from an exhausted budget.
- An in-flight *effect* is never abandoned half-recorded: its outcome is written before the loop
  notices the cancellation, or the effect never began. Demo with the ledger from 4.5.
- Wire it to `HttpContext.RequestAborted` in the 7.3 API and close the browser tab on camera.
- `.Budget(maxWallClock:)` as the other half: the timeout for when nobody is there to cancel — which
  is every trigger in 7.4 and 7.5.

**The gotcha**
Cancellation, budget exhaustion and refusal are **three different outcomes**, and a caller that
collapses them into "it didn't work" cannot decide whether to retry. Show all three arriving as
distinct statuses in one session.

**What changed in the shape** — nothing. A control that was always there, now demonstrated.

---

## 7.7 — Testing the agent you built

**Runtime** 13 min · **Branch** `series/s7e7-testing`

**Cold open**
> "Your agent passed code review. How do you know it still refuses what it refused last month?"

**Beats**
- The core problem: agent behaviour involves an LLM and is non-deterministic, so you cannot assert on
  it directly. You assert on the **deterministic contracts around it**.
- **Script the brain.** Replace the *broker*, never the service — the whole real agent stays under
  test. `.OnBrain(_ => ValueTask.FromResult("ACTION: calculator: 1+1"))` is the simplest double there
  is.
- Rule guardians in tests (`.RuleGate` / `.RuleJudge`) so guardian behaviour is pinned with no
  network call and no model variance in CI.
- Test *your* tools directly — they're plain `ITool` implementations, and `CompensateAsync` deserves
  a test more than `ExecuteAsync` does.
- **Test the trigger path too**, now that part 7 has given you several: a redelivered event must not
  double-effect (7.5), an unattended run must carry a principal (7.7). Both are contract tests and
  neither needs a model.
- Then borrow the framework's own trick: **sabotage-verify your test.** Break the behaviour, watch it
  go red, revert.

**The gotcha**
Testing that "the agent gives a good answer" is a trap — you'll be tuning assertions against model
drift forever. Test the **contracts**: which tool was called, whether the guardian refused, whether
the budget tripped, whether the effect was claimed once. Those are stable across model upgrades; the
prose isn't.

**What changed in the shape** — nothing. Doubles replace brokers, so the shape under test is real.

---

## 7.8 — Stability, upgrades, and what's in the box

**Runtime** 12 min · **Branch** none · **Docs** support.md

**Cold open**
> "Before you put this in a bank, someone is going to ask you four questions. Here are the answers."

**Beats**
- **What is stable** — the builder surface and the models are the contract. Service classes and their
  constructors are explicitly **not**, which is what let the entire architecture be rebuilt in 1.2
  without breaking a single consumer. Show that claim being cashed.
- **Deprecation** — nothing disappears without an `[Obsolete]` window. The worked example:
  `LocalBrain` / `LocalGate` / `LocalJudge` became `OnBrain` / `OnGate` / `OnJudge`, because a
  delegate you write is **Custom**, not Local — and the old names remain as behavioural aliases,
  pinned to the new ones by a test.
- **Upgrading** — read the version notes; they're written as prose in the `.csproj` and say what
  *kind* of change happened and why.
- **Standard Versioning**: `v1.2.3.4` = **model · service/routine · fix/config · build**. Deliberately
  **not** semver — the segments say what kind of change happened, so a model change can never hide in
  the service segment. Walk 1.0 → 1.4 and say what each bump meant.
- **Supply chain** — the core package is dependency-free by design; provider packages are opt-in and
  each brings exactly one backend. SBOM in the release artifacts (`bom.json`). Show it.

**The gotcha**
"Dependency-free core" is a load-bearing claim, not marketing. It's why a local GGUF, a Redis memory
and a Postgres knowledge store coexist without a version fight — each provider package brings its own
tree and nothing is forced on anyone who doesn't opt in. Contrast with a framework that pulls forty
transitive packages to say hello.

## 7.9 — Narration and the streamed outcome: the agent speaks while it works

**Runtime** 14 min · **Branch** `series/s7e9-narration` · **Docs** how-to §19–20

**Cold open**
> "Your agent just spent forty seconds researching. Your user watched a spinner. Same forty
> seconds, but narrated — 'Searching the live web…' at second two — and nobody left."

**Beats**
- Narration is a **channel**, not chatter: the model's prose beside its tool call rides
  `GenerationResult.Narration`; a silent decider gets the narration floor from the tool's own
  templates (`NarrationStarting`/`NarrationObserved`, `{tool}`/`{payload}` slots).
- Every model-authored line is screened through the Gate before it is voiced — a refused
  narration is withheld everywhere and recorded as WITHHELD. The user hears the agent, never an
  injection speaking through it.
- `RunStreamAsync` — the third door: every event live, and the enumeration's completion still
  carries the structured outcome (`status`, `result`, the pending effect with its call id). The
  caller never chooses between the answer's structure and the run's story.
- Bridge it to the 7.3 SSE endpoint: forward each event as it arrives, read `Outcome` for the
  terminal frame.

**The gotcha**
Narration made a latent bug *visible* in production within a day — a greeting was quietly
web-searching, and the moment the agent said so out loud, everyone saw it. Instrumentation that
talks to users is also instrumentation for you. (That story continues in 7.10.)

**What changed in the shape** — nothing. The loop always computed the outcome on the streamed
door; this release stopped discarding it.

---

## 7.10 — Selection, and its enforcement: offer a run only what its task needs

**Runtime** 15 min · **Branch** `series/s7e10-selection` · **Docs** how-to §21–22

**Cold open**
> "The day narration shipped, production watched a greeting run a web search six times. Not
> because the model needed it — because the turn was *shown* the tool."

**Beats**
- What an agent **carries** and what a run is **offered** are different things. Advertisement
  was composition-scoped and turn-blind; twenty tool servers meant twenty catalogs in front of
  "Hello", every turn, at token cost that scales with the catalog.
- `.OnSelectTools((task, described) => …)` — the host's judgment: a rule, a keyword table, a
  cheap classifier. Its output is read only as names; the Brain still decides among what was
  offered. The greeting is offered nothing, and answers as a greeting should.
- The decision log records the truth: `Selection → offered [web_search]; withheld [code_search,
  remember]` — including the built-in `remember` tool, which a stateless deployment withholds
  *knowingly*.
- **Then the sequel:** a custom brain — a gateway, a router — can carry side-channel knowledge
  of the catalog, and the same greeting kept searching *after* selection was live. Selection
  governs what a run is shown; it cannot govern what a Brain already knows.
- `.EnforceSelection()` — the offering **binds** at the Direction perimeter: an unoffered
  advertised tool is denied, told, non-terminal — `Selection → DENIED 'web_search': not offered
  to this run` — and the run recovers to an answer.
- The boundaries, each shown: caller tools never denied; undescribed tools keep their §6.1
  treatment; no selector, nothing to enforce; off by default.

**The gotcha**
Selection is a *judgment seam*, deliberately not a triad: one delegate in, names out, and an
empty selection is a valid selection. Don't go looking for `UseSelection(broker)` — a selector
that needs a provider package is a classifier, and that's what the delegate calls.

**What changed in the shape** — Decision's offering narrowed per run; Direction's perimeter
gained step 0. Both spec'd first (SPEC.md §4.15, v1.11–v1.12), both found in production.

---

**Part close** — "It starts the right way, stops when told, speaks while it works, offers each
run only what its task needs, is tested, and you can answer the architecture review. Last part
is for people who want to take it apart."
