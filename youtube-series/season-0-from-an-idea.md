# Season 0 — From an Idea

5 episodes · 9–13 min each · no code until 0.5, and that is deliberate

The series was missing its beginning. Everything from Season 1 on answers *how*; nothing answered
**should you, and what exactly**. That gap is where most agent projects actually die — not in the
framework, in the eighteen months before anyone opens an editor.

This season is also the part a book needs most, because it is the part a reader cannot get from
reference documentation.

---

## 0.1 — When an agent is the wrong answer

**Runtime** 11 min · **Branch** none

**Cold open**
> "The most valuable thing I can tell you about agents is when not to build one."

**Beats**
- The honest decision tree, in order of cost:
  1. **A script.** Deterministic input, deterministic output, no judgement. Build a script.
  2. **A workflow.** Known steps, known order, occasional branching. Build a workflow engine step.
  3. **Retrieval + a single model call.** A question over your documents with no action taken. That
     is RAG, and it is not an agent. Cheaper, faster, easier to evaluate.
  4. **An agent.** You need it when the *sequence is not known in advance* — when the next step
     depends on what the last one returned.
- The one-sentence test: **if you can draw the flowchart, you do not need an agent.** Agents are for
  when the flowchart has a loop in it whose exit condition is a judgement.
- Where agents genuinely win: variable-length tool use, ambiguous input, adaptive retrieval,
  human-in-the-loop escalation.
- Where they lose and people ship them anyway: anything with a hard latency SLA, anything requiring
  exact reproducibility, anything where a wrong answer is unrecoverable *and* unreviewable.
- The cost shape nobody models up front: an agent turn is *n* model calls, not one. Gate + brain +
  judge is three, times the turn count. Budget for 5–20×, not 1×.

**The gotcha**
"Agentic" is a procurement word now, and it will be asked for by people who mean "a chatbot with our
docs in it." Meet that honestly. Delivering RAG when RAG is right and saying so is worth more than
delivering an agent that impresses in a demo and can't hold an SLA.

**What changed in the shape** — nothing. This is the episode about not building.

---

## 0.2 — Designing the agent before you write it

**Runtime** 13 min · **Branch** none (whiteboard)

**Cold open**
> "Four questions. If you can't answer them, you're not ready to open an editor."

**Beats**
- The design worksheet, and it maps exactly onto the three natures:
  1. **What must it know?** → Data. Skills, knowledge, memory. Where does each live *today*?
  2. **What must it decide?** → Decision. What is a good answer, and what must never be answered?
  3. **What must it do?** → Direction. Every tool, and for each: **is it reversible?**
  4. **What must never happen?** → the perimeter. This is the question that sets the profile.
- Fill it in on camera for a real case — a support agent that can issue refunds.
- The reversibility column is the one that decides your whole architecture. Count the irreversible
  tools: **zero** → Core or Reliable is enough. **One or more** → you are going to Enterprise, and
  probably Critical, and you should plan for it now rather than retrofit it in month five.
- Pick the target profile *at design time* and write it down. It is a scope decision, not a
  maturity level you drift into.
- Sketch the first version deliberately smaller than the ambition: one skill, two tools, no
  guardians. Season 1 builds exactly that.

**The gotcha**
The commonest design error is putting a capability in the wrong nature, and it is almost always
memory-shaped: conversation history is **not** durable memory, and retrieved documents are **not**
either. Three different lifetimes, three different foundations. Getting this wrong is a rewrite;
getting it right is a worksheet.

**What changed in the shape** — you now know which parts of the shape you need.

---

## 0.3 — Choosing a model, and what it costs

**Runtime** 12 min · **Branch** none (spreadsheet + calculator)

**Cold open**
> "The model is the cheapest decision to change and the most expensive one to get wrong at scale."

**Beats**
- Four axes, scored honestly for a real workload: **capability, latency, cost, and where the data
  goes.** The fourth is a policy question, not an engineering one, and it frequently decides the
  other three for you.
- Build the cost model live in a spreadsheet: prompts/day × turns/prompt × calls/turn × tokens/call
  × rate. Then multiply by the guardian factor — a gated, judged agent is ~3× the calls of a naked
  one.
- **A small model for the guardians and a large one for the brain** is usually the right first
  answer, and it's the cheapest big win in the series. Classification is easier than generation.
- Where local wins outright: fixed cost, data never leaves, no rate limit, no vendor. Where it
  loses: capability ceiling and someone has to operate it.
- Hybrid, again, because it is what most regulated deployments land on: local for anything that
  touches customer data, hosted for anything that needs the frontier.
- Set the budget number *now* — `.Budget(maxCostUsd:)` is a design output, not a tuning knob you
  find later.

**The gotcha**
Benchmarks will not tell you which model works for *your* prompt. Season 7's testing episode is how
you actually decide: script the brain, pin the contracts, and swap models against the same suite.
Model selection is an evaluation problem, and treating it as a reading-comprehension problem is why
teams re-pick their model three times.

**What changed in the shape** — Decision has a backend and a budget, on paper.

---

## 0.4 — Writing skills that actually work

**Runtime** 12 min · **Branch** `series/s0e4-skills-design`

**Cold open**
> "Your agent's instructions are a product surface. They're just written in Markdown instead of
> C#."

**Beats**
- This is the one place the series teaches prompt *craft*, because skills are where it lives — and
  it belongs before Season 2 shows the mechanism.
- What goes in a skill: identity, scope, tone, refusal boundaries, and the shape of a good answer.
- What does **not** go in a skill: safety rules that must hold (those are the Gate — a skill can be
  argued with), and facts that change (those are Knowledge).
- One concern per file. Skills compose, and a 900-line monolith cannot be reviewed by the person who
  should own it.
- Write for the reader who will edit it next, who is frequently not an engineer.
- Version them. A skill change is a behaviour change and deserves the same review as a code change —
  which is the argument for the registry mode in 2.1.
- Test that a skill did what you meant: assert the *contract* (did it refuse, did it call the right
  tool), never the prose.

**The gotcha**
An instruction in a skill is a **request**, not a constraint. The model may ignore it, and a
determined user may talk it out of it. If a rule must hold, it belongs in the Gate or the policy,
not the persona. Every serious incident in this space starts with someone putting a security control
in a prompt.

**What changed in the shape** — Data, designed before it's built.

---

## 0.5 — From worksheet to a running skeleton

**Runtime** 10 min · **Branch** `series/s0e5-skeleton`

**Cold open**
> "Twenty minutes from the worksheet to something running. It does almost nothing, and that's
> correct."

**Beats**
- Take 0.2's worksheet and stand up the skeleton: a brain, one skill, one **read-only** tool, a
  trace, and nothing else.
- Read-only on purpose. The first version must not be able to do anything you'd regret, so the
  perimeter can arrive on schedule rather than under pressure.
- Wire the trace from minute one, not later. You cannot debug what you cannot see, and every
  subsequent episode reads that trace.
- Write the first contract test before the second feature: one refusal you care about, one tool that
  must be called. It costs ten minutes now and anchors every model swap afterwards.
- Set `.MaxTurns()` and `.Budget()` immediately, even generously. An unbounded loop in development
  is how people discover budgets by receiving an invoice.
- Then the roadmap: point at the profile chosen in 0.2 and name which seasons get you there.

**The gotcha**
Ship the skeleton to one real user before adding anything. Every capability from Season 2 onwards is
one line, so there is no architectural reason to front-load them — and the feedback from a real
prompt will change your worksheet more than any amount of planning will.

**Season close** — "You know what you're building, what it costs, and what it must never do. Now
let's make it talk."
