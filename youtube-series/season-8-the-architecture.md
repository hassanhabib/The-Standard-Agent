# Season 8 — The Architecture

9 episodes · 10–15 min each · separate playlist, different audience

Seasons 1–5 were for people *using* the framework. This one is for people **porting it, extending
it, reviewing it, or deciding whether to trust it** — and for anyone who builds enterprise software
in any language, because almost nothing here is C#-specific.

It opens with the audit rather than the diagram. A season that starts with "here are the tiers" gets
skipped; a season that starts with "here are five violations that shipped, and nothing caught them"
does not.

---

## 8.1 — The audit: five violations that had already shipped

**Runtime** 14 min · **Branch** `series/s8e1-audit` · **Source** `docs/architecture-alignment.md`

**Cold open**
> "The diagram in the README described one architecture. The code was a different one. Nobody
> noticed for eight releases."

**Beats**
- Audit the shipped diagram against the shipped code, live. Find **five brokers in tiers where a
  broker is not allowed**: an orchestration holding three, a coordination holding two, a broker
  holding two more.
- Every one arrived with the enterprise program. None was caught, because **nothing was checking**.
- The cost was not stylistic: a tier calling a broker directly skips the foundation that would have
  given it validation and exception mapping — so a full disk in the effect ledger surfaced as a raw
  `IOException` **blamed on Direction.** Show that misattribution.
- Set up the season's question: what would have had to be true for the build to catch this?

**The gotcha**
Every one of the five cost nothing visible on the day it was introduced. That's not incidental —
it's the mechanism. Erosion that hurt immediately would get fixed immediately.

---

## 8.2 — Brokers: one liaison, one resource

**Runtime** 11 min · **Branch** `series/s8e2-brokers`

**Beats**
- A broker is a **thin liaison to exactly one external resource.** No business logic, no branching
  on domain concepts.
- The three kinds, and why the distinction is load-bearing:
  - **Nature brokers** (13) — one per foundation. Skill, Knowledge, Memory, Session, Generator,
    Usage, Classifier, Verifier, Policy, Approval, EffectLedger, Tool, Mcp.
  - **Utility brokers** (3) — logging, time, audit. Held by any tier. Exempt because **none of them
    can change what the agent decides or does.** That sentence is the test.
  - **Decorating brokers** (2) — redaction, resilience. Wrapped around another broker at
    composition, so no service holds them.
- Write a broker on camera. It should be boring, and take four minutes.
- `ReturnService` has no broker at all — the dead end. The terminal Direction hands the result back
  and touches nothing.

**The gotcha**
Resilience is deliberately **not** a utility even though it looks like one: it changes control flow,
so it doesn't get logging's exemption. Compare with redaction, which transforms the payload — a real
argument exists that a payload-rewriting decorator is doing more than a thin liaison should. Present
both sides; this is a live design tension, not settled dogma.

---

## 8.3 — Foundations: one broker each, and why

**Runtime** 12 min · **Branch** `series/s8e3-foundations`

**Beats**
- A foundation wraps **exactly one** nature broker and supplies the three things a raw broker call
  is missing: **validation, exception mapping, attribution.**
- Build `UsageService` from nothing, in full — it's the smallest complete example in the codebase:
  interface, service, `.Validations.cs`, `.Exceptions.cs`, exception models.
- The validations that aren't ceremony: null text is a caller that lost what it meant to charge for;
  a **negative** token count subtracts from the run's spend, turning a budget into something an
  unusual reply can *extend* rather than exhaust. A custom counter is host code, so that one is
  reachable.
- Fourteen foundations. Ten always present, four optional.

**The gotcha**
The role may be **versioned** — `IGeneratorBrokerV1` is the same seam as `IGeneratorBroker` under a
newer contract, so one foundation may hold both while speaking to only one at a time. That's one
broker *role*, not two brokers, and the enforcement test has to know the difference or it produces
false positives. It did, once.

---

## 8.4 — Orchestrations, regions, and the 2–3 rule

**Runtime** 13 min · **Branch** `series/s8e4-regions`

**Beats**
- **Every tier holds two or three of the tier directly below it.**
- Both bounds cost something when broken:
  - More than three → the service is doing too much. Direction had **six** foundations.
  - Fewer than two → it composes nothing. It's a layer and an exception hop for no work.
- **Regionalization**: six regions, named for concepts rather than contents — Retrieval /
  Recollection, Inference / Guardian, Perimeter / Execution.
- **Conceptual normalization**: Direction was two concepts — *may it happen* versus *do it*. Once you
  see that, the split is obvious and the names write themselves.
- Walk the six and say what single question each answers.

**The gotcha**
`InferenceOrchestrationService` shipped for a whole release holding **one** foundation, and its own
interface carried a comment arguing it was "a region rather than a pass-through." That is what
arguing around a rule looks like from the inside — a defence written into the code by the person who
broke it. The fix wasn't to collapse it; it was to notice that Inference could say what the model
*said* and not what it *cost*, and that the missing thing was a foundation.

---

## 8.5 — Coordinations, the loop, and what tier a thing belongs to

**Runtime** 12 min · **Branch** `series/s8e5-loop`

**Beats**
- Three coordinations, one per nature, each over two regions.
- `RunManagementService` — the only loop: Recall → Think → Act, one run.
- Read the loop body aloud. It's short, and it's the whole agent.
- Turn boundaries as the unit everything else is defined against: budgets checked there,
  cancellation honoured there, sessions written there.
- **Screening lives in the loop**, and the reasoning generalises: *what may enter the context between
  turns is the loop's question, and the loop is the only place that sees both natures.*
- Where a capability belongs, as a decision procedure a viewer can reuse in their own system.

**The gotcha — the best story in the season**
Screening used to sit in `DirectionCoordinationService`, holding Decision's Gate. It passed **every
rule the build was checking** — three dependencies, no broker above the foundation tier. Nothing
about the count was wrong; the **direction of the reach** was. No test of counts or brokers was ever
going to catch it, which is the entire argument for writing tier-adjacency down as a rule of its own
rather than treating it as something the count implies.

---

## 8.6 — Exceptions: the Xeption model

**Runtime** 11 min · **Branch** `series/s8e6-exceptions`

**Beats**
- The families: `Validation`, `DependencyValidation`, `Dependency`, `Service` — and what each tells a
  caller about **whether to retry**.
- `TryCatch` as a partial class per service. Read one.
- **Localize once.** A failure is mapped at the tier that owns the resource and passes through
  above it — wrapping twice nests the exception and logs the same failure twice. Show the
  already-localized passthrough and what it prevents.
- Why the categories matter operationally: retry decisions are made on **category, not message**
  (5.1), and that only works if the categories are honest.
- Exception *models* per foundation, six or seven small files. Boring on purpose.

**The gotcha**
The whole point of foundations, restated with evidence: a full disk in the effect ledger arrived as a
raw `IOException` attributed to Direction, because a tier reached the broker directly and skipped the
mapping. The taxonomy is not bookkeeping — it's what makes a stack trace name the right subsystem at
3am.

---

## 8.7 — FAIL/PASS TDD and sabotage verification

**Runtime** 13 min · **Branch** `series/s8e7-tdd`

**Beats**
- Two commits per test. The **FAIL** commit contains a test that has been *run and observed failing*
  — not one assumed to fail.
- Do it live: write a failing test, run it, commit, implement, run, commit.
- Brokers carry **no** unit tests. They're thin liaisons; testing them tests the SDK.
- **Sabotage verification** for anything already written: break the behaviour, watch the test go red,
  revert. Demonstrate on a test written after its implementation — which is the case where it's not
  optional.
- Commit message discipline: the message explains *why*, at length, because the diff already shows
  the what.

**The gotcha**
The honest admission belongs on camera: `UsageService`'s tests were written **after** the
implementation, so FAIL-first was never observed. All eight were sabotage-verified instead, and the
commit says so plainly rather than letting it look like discipline that was followed. Showing that
teaches more than a clean example would.

---

## 8.8 — Enforcing architecture with tests

**Runtime** 14 min · **Branch** `series/s8e8-tierdiscipline`

**Beats**
- `TierDisciplineTests` — the rules, as a test rather than as a convention. Read all four:
  1. A foundation wraps exactly one nature broker (role may be versioned).
  2. Nothing above the foundation tier takes a broker, beyond the three utilities.
  3. No broker depends on another nature's broker.
  4. Every tier holds 2–3 of the tier **directly** below it.
- Write rule 4 from scratch on camera, then sabotage-verify it with a throwaway one-dependency
  orchestration and watch it name the service and the count.
- Reflection over constructors, **per constructor rather than per type** — Memory and Knowledge each
  offer two one-broker overloads, and counting across them reads as two and is wrong.
- Generalise hard: **this technique is language-agnostic and the most portable thing in the series.**
  Any codebase with layering rules can enforce them this way, and almost none do.

**The gotcha**
Version 1.2 shipped an enforcement suite that checked the rule it could *see* and not the rule it
*existed for*. It enforced "no brokers above foundations" and said nothing about counts — so the 2–3
rule, the thing the entire realignment was carried out to honour, was the one rule nothing was
checking. Ask the audience the question that follows: *what is your test suite not checking because
nobody thought to write it down?*

---

## 8.9 — Porting it: the spec is the product

**Runtime** 15 min · **Branch** none (spec + vectors)

**Beats**
- `SPEC.md` is **normative and language-neutral**. The C# library is the reference implementation,
  not the definition.
- Conformance is about **contracts and behaviour, not file layout or language idiom** — you can port
  this to Go or Python without copying the folder structure.
- The two rulebooks and how they compose: SPEC owns contracts and behaviour; The Standard owns
  structure, exceptions and process.
- Walk a port: pick a nature, implement its broker seam, run the vectors, watch a profile go green.
- RFC 2119 keywords, profiles, and the conformance checklist as an actual to-do list.

**The gotcha — end the series on this**
SPEC 1.0 said a budget must be measured against *reported* usage and said **nothing** about the case
where a provider reports none. That silence was read as permission to contribute zero, and the
reference implementation enforced no token budget on the text protocol for eight releases while
passing every vector and claiming the Enterprise profile.

> **A specification that is silent where an implementer will guess has not settled the question. It
> has moved it.**

SPEC 1.1 states it outright, and two new vectors certify it — both proven able to fail before being
added. That's the closing note for the whole series: the value of a spec isn't that it reads well,
it's whether someone who has never seen your code can build one that passes the same tests.

**Series close** — "Sixteen capabilities, three verbs each, fourteen foundations, one loop. And an
architecture that fails the build when it drifts, because the only rules that survive are the ones
something is checking."
