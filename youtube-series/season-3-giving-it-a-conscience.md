# Season 3 — Giving It a Conscience

6 episodes · 10–14 min each · lands the **Reliable** profile

The Decision nature, properly. A brain that answers is easy; a brain that can be *stopped before it
runs* and *overruled after it does* is what separates a demo from a deployment.

The framing for the whole season: **a guardian is a classifier, never an author.** Everything here
follows from that one sentence, including why the guardians can't be talked into answering.

---

## 3.1 — The Gate: a conscience before the brain

**Runtime** 15 min · **Branch** `series/s3e1-gate` · **Docs** how-to §4

**Cold open**
> "Some prompts should never reach your model at all. Not filtered afterwards — never sent."

**Beats**
- `.Gate(apiUrl, apiKey, model)` — screening before the brain runs.
- Send something the gate should refuse. Watch it stop *before* a brain call. Show the trace so the
  viewer sees the brain was never invoked — that's the cost argument and the safety argument at once.
- The Gate runs its **own rubric**, not the agent's prompt. This matters: the agent's persona can't
  soften its own screening.
- Verdicts are classifications — `allow`, `refuse`, `route`. Not prose.
- A refusal is non-terminal and never silent: the agent says what happened rather than pretending
  the request didn't exist.

**All three modes — all demonstrated (+3 min)**
- **Local** `.RuleGate(...)` — deterministic, free, instant. Covered in depth in 3.3.
- **External** `.Gate(apiUrl, apiKey, model)`, or `.UseGate(IClassifierBroker)` for any classifier.
- **Custom** `.OnGate(delegate)` — your own screening code, or a local model's `GenerateAsync`.

`.UseGate` taking a raw `IClassifierBroker` is the seam that lets a purpose-built safety classifier — not a chat model — do the screening. Show it.

**The gotcha**
The Gate is a **model call with a cost and a latency**. It roughly doubles the per-prompt spend on a
naive implementation. The framework screens an unchanged prompt **once per run**, not once per turn
— the verdict is remembered on the run, not in a service-level cache, so it's evicted when the run
ends rather than leaking on a busy day. Show the trace proving the second turn doesn't re-screen.

**What changed in the shape** — Decision gained a foundation.

---

## 3.2 — The Judge: a conscience after the brain

**Runtime** 16 min · **Branch** `series/s3e2-judge` · **Docs** how-to §5

**Cold open**
> "The model answered. That doesn't mean the answer is good enough to send."

**Beats**
- `.Judge(apiUrl, apiKey, model)` — the draft is scored before it's an answer.
- The revision loop: a rejection is a **re-think signal** (`AgentStatus.Revising`), not a fault. The
  loop retries within the turn budget.
- What happens when it still can't pass: a graceful refusal — *"I can't help with that at the
  moment"* — rather than an exception or an empty string.
- `MinimumAcceptableScore` and what tuning it actually trades away.
- Streaming parity again, now that it means something: the draft streams as `Thinking`, and only a
  Judge-settled answer becomes `Response`. A rejected draft never leaks to the user.

**All three modes — all demonstrated (+3 min)**
- **Local** `.RuleJudge(...)` — thresholds and rules, no model.
- **External** `.Judge(apiUrl, apiKey, model)`, or `.UseJudge(IVerifierBroker)`.
- **Custom** `.OnJudge(delegate)` — including a *different* model from the brain, which is the point of 3.6.

Judge is the capability where Custom is most often right in production: scoring rubrics are domain-specific and rarely fit a generic model call.

**The gotcha**
Gate and Judge are **one concept at two moments**, which is why the framework calls them the
guardians and composes one rubric for both. If you find yourself wanting different ethics for input
and output, you want a Constitution (3.4), not two philosophies.

**What changed in the shape** — Decision, second foundation. Decision is now Brain, Gate, Judge.

---

## 3.3 — Rule guardians: no model required

**Runtime** 10 min · **Branch** `series/s3e3-ruleguardians`

**Cold open**
> "Not every refusal needs a language model. Some need a regular expression and zero milliseconds."

**Beats**
- `.RuleGate(...)` and `.RuleJudge(...)` — the **Local** mode of both guardians.
- Deterministic, free, instant, and testable — no model variance in your test suite.
- The pattern that works well in production: rules first for the obvious cases, model second for the
  ambiguous ones. Cheap filter, expensive filter.
- Use them in unit tests so guardian behaviour is pinned without a network call. Show a test.

**The gotcha**
Rules cannot catch intent, only surface form. A rule gate is a **cost optimisation and a
determinism guarantee**, not a safety upgrade. Anyone who ships rules alone and calls it guarded has
misunderstood which problem each solves.

**What changed in the shape** — nothing. Two backends for foundations that already existed.

---

## 3.4 — Constitution and Consumption: one law above both

**Runtime** 12 min · **Branch** `series/s3e4-constitution`

**Cold open**
> "Your gate and your judge disagree about what's acceptable. That's not a config problem, it's a
> governance problem."

**Beats**
- `.Constitution("Constitution/ethics.md")` — an ethical charter prepended above **both** guardian
  rubrics.
- `.Consumption("policy.md")` — swaps a domain policy in for the built-in one.
- The layering, drawn: **constitution → policy → framework contract**, top to bottom.
- The framework-owned output contract always stays *below* either, so a replacement policy can
  never break the guardian's wiring. Demonstrate by writing a deliberately hostile policy and
  showing the verdict format survive.
- Both inputs are optional and degrade to the built-in policy if absent.

**The gotcha**
This is why gate and judge prompts are split into a **replaceable policy** and a **fixed contract**.
Without that split, letting someone edit the policy would let them break the parser. Governance and
mechanism have to be separable or you can't safely delegate the governance.

**What changed in the shape** — nothing structural; both guardians read a shared rubric.

---

## 3.5 — When your skills contradict each other

**Runtime** 12 min · **Branch** `series/s3e5-conflict`

**Cold open**
> "One skill file says always escalate. Another says never escalate. Which one wins?"

**Beats**
- Write two genuinely contradictory skills on camera.
- Without conflict detection, one silently wins and you never find out which.
- The Gate detects direct contradiction across active skill instructions (`DetectConflictAsync`).
- Instead of picking, the agent **asks** — `AgentStatus.AwaitingInput` — with the options.
- The answer is learned as a durable memory preference, so future identical conflicts resolve
  without asking. Show the second run not asking.

**The gotcha**
This is Decision *and* Data cooperating: the Gate detects, and Memory carries the learned preference
forward. It's the first episode where the natures visibly work together, and it's a good moment to
point out that neither one reaches into the other — the loop sequences them.

**What changed in the shape** — a Decision routine writing a Data fact, through the loop.

---

## 3.6 — Guardians that scale: an agent as a tool

**Runtime** 14 min · **Branch** `series/s3e6-fractal`

**Cold open**
> "The same model grading its own homework is not a second opinion."

**Beats**
- Callback to 2.7: one GGUF as brain, gate and judge is cheap and **correlated**.
- The fractal: an agent satisfies `ITool`, so an agent can be a tool of another agent.
  ```csharp
  var researcher = new AgentTool("researcher", innerAgent);
  var outerAgent = new StandardAgent().Brain(...).Tool(researcher);
  ```
- Build a **compliance sub-agent** with its own skills, its own constitution, and a different model,
  then use it as a distinct conscience.
- Why nesting needs no new machinery: the shapes already agree. Turtles up.
- Where it earns its keep: specialist review, domain separation, and a genuinely independent
  adversarial check.

**The gotcha**
Every nested agent is a full agent — its own turns, its own budget, its own guardians. Costs
compound multiplicatively, not additively. Budget the inner agent explicitly (4.7) or a single outer
prompt can quietly fan out into dozens of model calls.

**Season close** — "It can refuse, and it can explain itself. It still cannot be trusted with your
customer's money. Next season: identity, redaction, approval, and acts that can only happen once."
