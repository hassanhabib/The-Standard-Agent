# Season 5 — Mission Critical

7 episodes · 12–18 min, capstone ~25 min · lands the **Critical** profile

Everything so far assumed the happy path eventually arrives. This season assumes it doesn't: the
provider is down, the process dies, the act went out and cannot be taken back.

The framing: **Critical is the level a wire transfer needs.** Every episode is judged against that.

---

## 5.1 — Resilience: surviving a bad afternoon

**Runtime** 16 min · **Branch** `series/s5e1-resilience` · **Docs** how-to §13

**Cold open**
> "The provider returned a 503. Your agent returned an exception to a customer."

**Beats**
- `.Resilience(retries: 3)` — bounded retry with backoff and jitter.
- **What is retryable is decided by the error's category, not its text.** A dependency failure is
  retryable; a validation failure never is, because retrying it will fail identically and only
  spends the budget. Show both, and show a naive string-matching retry getting it wrong.
- Jitter matters when many agents share a provider — say why in one sentence.
- **A retried call is still one turn.** Retrying must not consume the turn budget, because a turn is
  a unit of the agent's reasoning, not of the network's luck.
- `.Fallback(...)` — degrade to a configured alternative rather than fail outright. A degraded
  answer is worth more than no answer.
- Circuit breaking: `FailuresBeforeOpen`, and health tracking that **must not change any verdict
  while the primary is healthy.**
- An implementation with no alternative configured **fails rather than pretends.** Silently returning
  an empty or fabricated result is worse than an error, because the caller cannot tell.

**All three modes — all demonstrated (+3 min)**
- **Local** `.Resilience(retries: 3)` — bounded retry with backoff and jitter.
- **External** `.UseResilience(IResilienceBroker)` — hand it Polly, or your platform's policy.
- **Custom** `.Fallback(...)` — your own degradation path when the primary is unhealthy.

Resilience is the one row where the Custom verb is not `.OnX`, and there is a reason: falling back is a different decision from retrying, and naming it `.OnResilience` would have hidden that.

**The gotcha**
Retry meets run-once here, and 4.5 already paid for the setup: **retries without a ledger are a way
to pay twice.** Show the ledger suppressing a duplicate on a retried effect.

**What changed in the shape** — resilience is a decorating broker, like redaction. No new foundation.
It changes control flow, so it does *not* get the utility exemption.

---

## 5.2 — Compensation: undoing what cannot be repeated

**Runtime** 15 min · **Branch** `series/s5e2-compensation` · **Docs** how-to §14

**Cold open**
> "It booked the flight. Then it charged the card. Then it crashed. The flight is still booked."

**Beats**
- Some effects cannot be made idempotent at all. They can only be **unwound**.
- `ITool.CompensateAsync` — a tool declares how it is undone.
- `.CompensateOnFailure()` — a failed run unwinds in **reverse order** over what it *actually
  performed*, not what it intended.
- Build a two-step booking (book flight → charge card), fail the second, watch the first unwind.
- A tool that declares no way back is **reported as an effect that stands**, rather than silently
  counted as undone. That honesty is the feature.
- `CompensationOutcome` — what the caller learns.

**The gotcha**
Compensation is not a transaction. There is no rollback and no isolation — you are issuing a second
real-world act that attempts to negate the first, and it can fail too. Distinguish "compensated",
"stands", and "compensation failed", and make sure the viewer understands all three are possible
outcomes of a single run.

**What changed in the shape** — Direction, using the ledger's record of what really happened.

---

## 5.3 — Native tool calling

**Runtime** 16 min · **Branch** `series/s5e3-native` · **Docs** how-to §15

**Cold open**
> "Your model provider has a structured tool-calling API. You've been parsing the first line of a
> string this whole time."

**Beats**
- `.UseNativeBrain(...)` / `.OnNativeBrain(...)` — the V1 contract.
- V0 (text protocol) vs V1 (native): the model's choice arrives as **structured data** rather than as
  the first line of its text.
- **Round-tripping** is the thing V0 cannot express: a result comes back as a tool message naming
  the call that asked for it. Show `ToolCallId` and `ToolExchange` doing that work.
- One call per turn, deliberately: a model may ask for several, but Direction performs one act at a
  time, because authorization, approval and run-once are judgments about a *single* act. The rest
  are re-proposed next turn, and run-once makes a repeat free.
- Everything after interpretation is identical — Judge, perimeter, budget, ledger. **Adopting native
  calls changes how a choice is read, not what the agent is.**

**All three modes — and the documented gap (+3 min)**
- **Local** — **none**, and this is the second of the framework's only two dashes. Running a model in-process needs an inference runtime; the core is dependency-free by design.
- **External** `.UseNativeBrain(broker)`, or `.NativeBrain(...)` for an endpoint by URL.
- **Custom** `.OnNativeBrain(delegate)` — structured tool calls from your own runtime.

Put the waiver from `StandardAgentCapabilityTests` on screen. A gap with a reason written into the test that enforces the rule is a different thing from a gap nobody logged.

**The gotcha**
**V0 is not deprecated and never will be.** It's the contract that works against *any* endpoint —
which makes it the likelier half of a real estate to be running. Two consequences the series has
already met: this is why usage counting had to work on V0 (4.7), and it's why a V1 endpoint that
omits its usage object still gets a bounded budget.

**What changed in the shape** — one broker role, versioned. `IGeneratorBrokerV1` is the same seam as
`IGeneratorBroker` under a newer contract, which is why one foundation may hold both.

---

## 5.4 — Crash recovery: a run is not confined to a process

**Runtime** 15 min · **Branch** `series/s5e4-continuity`

**Cold open**
> "Pull the plug in the middle of a wire transfer. Now bring it back."

**Beats**
- `AgentSession.RunId`, written at the **start** of the run — because a crash means nothing at the
  end runs at all.
- A session that never delivered an answer is **resumed with the interrupted run's identity**, so
  the idempotency keys still match.
- The full demo, on camera, unedited: start a run with an irreversible act → kill the process
  mid-flight → restart → resume the session → watch the already-performed act replay instead of
  repeat.
- `FileEffectLedgerBroker` makes run-once outlive the process that claimed it.
- How this composes with approval: an act held for an authority survives the restart, and the
  approval is still meaningful when it arrives.

**The gotcha**
This is the mechanism SPEC 1.0 required results from **without describing** — run continuity across
processes. It was found by building the thing rather than by rereading the prose, and it's now
written down. Good moment to say what a specification is *for*: not to read well, but to let someone
who has never seen your code build one that passes the same tests.

**What changed in the shape** — Data's Session foundation carries a run, not just a conversation.

---

## 5.5 — Readiness profiles: claiming a level, and proving it

**Runtime** 12 min · **Branch** `series/s5e5-profiles`

**Cold open**
> "Anyone can say their agent is enterprise-ready. Here's a command that decides."

**Beats**
- The four profiles and what each actually promises:
  - **Core** — conversation, skills, knowledge, memory, simple tools.
  - **Reliable** — guardians that see what they guard, durable decision log, run isolation,
    cancellation, timeouts.
  - **Enterprise** — identity-aware authorization, approval before irreversible acts, run-once
    effects, budgets, ranked retrieval.
  - **Critical** — conversation and effects that survive a process, compensation, native tool calls
    that round-trip.
- ```bash
  dotnet run --project Standard.Agents.Conformance -- --profile Critical
  ```
- Exit `0` means certified. **The runner is the authority, not the README table.**
- Run it against the viewer's own agent from season 4 and find out honestly where it lands.
- Profiles inherit: Critical ⊃ Enterprise ⊃ Reliable ⊃ Core.

**The gotcha**
`reliable.json` lists requirements that have **no vector yet, on purpose.** A level is earned by
evidence, and naming the evidence before it exists is what stops the level being claimed early.
That's an unusual and honest thing for a project to do — show the file.

**What changed in the shape** — nothing. This is how you check the shape.

---

## 5.6 — Conformance, and proving a test can fail

**Runtime** 14 min · **Branch** `series/s5e6-conformance`

**Cold open**
> "A test that cannot fail proves nothing. Two of the tests in this repo couldn't, and shipped
> anyway."

**Beats**
- How the vectors work: agent behaviour involves an LLM and is non-deterministic, so it cannot be
  asserted directly. The vectors pin the **deterministic** contracts by scripting the Brain.
- **Every double replaces a broker, never a service** — the whole 1·3·6·14 under test is the real
  library.
- Write a vector on camera.
- **Sabotage verification**: break the behaviour, watch the vector go red, revert. Do it live.
- The two vacuous vectors, named: `knowledge-retrieves-by-relevance` passed with the ranking
  inverted; `redaction-covers-every-model-call` passed while only the Brain's input was recorded.
  Both were caught this way and rewritten.
- The sharper lesson: **a missing vector costs more than a weak one, because nothing goes red for
  it.** Until 1.4 the only budget a vector could express was wall clock — so an implementation in
  any language could reproduce the token-budget defect exactly and still earn the Enterprise badge.

**The gotcha**
If you add a vector, break the thing it covers *first*. Not after. This is the rule the repo enforces
on itself and the single most transferable practice in the whole series.

**What changed in the shape** — nothing. This is how you prove the shape.

---

## 5.7 — Capstone: a wire transfer, end to end

**Runtime** ~25 min · **Branch** `series/s5e7-capstone`

**Cold open**
> "Everything, at once, doing the one thing you'd never let an agent do."

**Beats**
- Build it from empty, narrating which nature each line touches:
  ```csharp
  var agent = new StandardAgent(url, key, "LLooMA2.0")
      .Skills("Skills")
      .Tool(new WireTransferTool())
      .Gate(apiUrl: url, apiKey: key, model: "LLooMA2.0")
      .Judge(apiUrl: url, apiKey: key, model: "LLooMA2.0")
      .Memory("memory.txt")
      .Constitution("Constitution/ethics.md")
      .Redact()
      .LogTo("log.txt", TraceVerbosity.Full)
      .Audit("audit.jsonl")

      .Principal(() => currentUser.Id)
      .OnPolicy(Authorize)
      .RequireApproval("wire_transfer")
      .EffectLedger("ledger")
      .ScreenToolOutput()
      .Sessions("sessions")
      .Budget(maxCostUsd: 0.25m)
      .Resilience(retries: 3)
      .CompensateOnFailure();
  ```
- Then break it, deliberately, one failure at a time, and show the agent surviving each:
  1. A prompt the Gate refuses.
  2. A draft the Judge rejects, then a revision that passes.
  3. A tool result carrying an injection.
  4. An act requiring approval — held, approved out-of-band, resumed.
  5. A transient provider failure — retried, and **not** paid for twice.
  6. A process kill mid-transfer — resumed, replayed, not repeated.
  7. A budget exhaustion — reported distinguishably from a refusal.
  8. A failed run with a performed effect — unwound in reverse.
- Read the audit log at the end and reconstruct the entire decision from it.
- Certify: `--profile Critical`, exit 0.
- **Then delete every line below the blank one and run it again.** Same agent, less power, still
  works. That is the collapsible substrate, demonstrated rather than asserted.

**The gotcha**
Everything below the blank line is opt-in, each with a sane default, and **none of them is a new
concept**. Two that most obviously "wanted" a new tier didn't get one: the effect envelope is a model
spread across four existing seams, and telemetry is the audit broker with a different sink. Both
would have been easier as new services. Both would have cost the mark.

**Season close** — "That's the framework, used. Next season is the framework, *built* — and it opens
with an audit that found five violations in code that had already shipped."
