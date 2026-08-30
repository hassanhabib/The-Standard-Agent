# The book

**Working title:** *The Standard for Agents — From an Idea to a System You Can Put in a Bank*

The season files are shooting plans. This is the other half of the same material, and it is **not
the scripts reformatted**. A book and a video series fail in opposite directions, so they are
written to different strengths.

---

## What the book does that the video cannot

- **Rationale at length.** Video has to keep moving; a book can spend four pages on why redaction is
  a decorating broker and what the alternative would have cost. The *why* is the durable half of
  this material, and it is the half video compresses hardest.
- **Complete listings.** A reader can study a 90-line composition root at their own pace. A viewer
  cannot.
- **Reference tables you return to.** The capability matrix, the exception families, the profile
  requirements. Nobody scrubs a video for a table.
- **The full argument of an architecture.** Part VIII is a book chapter in a way it is barely a
  video: the audit, the reasoning, the corrections, the tests that now prevent recurrence.
- **Exercises.** Every chapter ends with two or three, and the answers are branches in the companion
  repo.

## What the video does that the book cannot

- **Failure, live.** Killing a process mid-transfer and watching it resume is worth more on screen
  than in any amount of prose.
- **Latency you can feel.** Remote versus local is a *sensation* before it is a table.
- **The trace, read aloud**, with a cursor moving down it.
- **Proof.** Running the conformance suite, breaking a behaviour, watching it go red.

**The rule:** where the video shows, the book explains; where the video asserts, the book proves.
Neither says "as you saw in the video" or "see chapter 4" — each stands alone.

---

## Structure

Nine parts, matching the nine parts of the series, ~38 chapters.

| Part | Chapters | From the season | Weight |
|---|---|---|---|
| **I · Deciding** | 1–4 | Season 0 | The part practitioners skip and then need |
| **II · The First Agent** | 5–8 | Season 1 | Fast. Get them running by chapter 6. |
| **III · Substance** | 9–12 | Season 2 | Data nature, in full |
| **IV · Conscience** | 13–15 | Season 3 | Decision nature |
| **V · The Perimeter** | 16–20 | Season 4 | The longest part. Enterprise buys the book for this. |
| **VI · Mission Critical** | 21–24 | Season 5 | Failure, recovery, proof |
| **VII · Systems of Agents** | 25–28 | Season 6 | The destination |
| **VIII · Triggering and Operating** | 29–35 | Part 7 | Triggers, lifetime, hosting, narration, selection, testing, upgrades |
| **IX · The Architecture** | 36–38 | Part 8 | For porters, reviewers, and the curious |

**Appendices** — and these are the pages that get dog-eared:

- **A · The capability matrix.** All nineteen, three modes each, exact signatures, the two
  documented gaps with their reasons — and the loop capabilities (narration, streamed outcome,
  selection, enforcement) with why they are deliberately not triads.
- **B · The readiness profiles.** What each requires, verbatim, and how to certify.
- **C · The conformance vectors.** All 69 (and counting — selection added one), what each pins,
  and how to write your own.
- **D · Exception families.** What each tells a caller about whether to retry.
- **E · The design worksheet** from chapter 2, as a one-page form.
- **F · Standard Versioning.** `v1.2.3.4`, and why it is deliberately not semver.
- **G · Porting checklist.** SPEC 1.1's requirements as a to-do list.

---

## Chapter conventions

Every chapter:

1. **Opens with a failure** — the same cold open as the episode, in prose. The problem before the
   feature, always.
2. **Builds it**, with complete listings, not fragments.
3. **Shows all three modes** — Local, External, Custom. Non-negotiable, exactly as in the video
   (see `capability-matrix.md`). In the book this is a table plus three short listings.
4. **"Why it is built this way"** — the section that has no video equivalent, and the reason the
   book exists. One to four pages.
5. **"What this cost us"** — where the framework got it wrong first. The principal that reached the
   audit record but not the decision. The budget that did nothing on V0 for eight releases. The
   vacuous vectors. These are the most valuable pages in the book and the most tempting to cut.
6. **Exercises**, with repo branches.

---

## The seven chapters that sell the book

Write these first, at full quality, and the rest follows their standard:

1. **Ch. 2 — Designing the agent before you write it.** The worksheet, the reversibility column, and
   choosing a profile at design time. This is the chapter a reader photocopies.
2. **Ch. 17 — Run once, even across a crash.** Atomicity as the whole mechanism, and *retries
   without a ledger are a way to pay twice.*
3. **Ch. 20 — Budgets that actually bound.** The eight-release defect, why the spec permitted it,
   and what a specification is for. The best cautionary tale in the material.
4. **Ch. 27 — Budgets, identity and approval across agents.** The propagation gap. The chapter that
   decides whether an enterprise trusts a multi-agent build.
5. **Ch. 32 — Webhook and event: untrusted at the front door.** The at-least-once trap, and the
   trap inside it: run-once is keyed on the run id, so a redelivered event performs the effect
   twice — and binding the session id does NOT fix it, because run continuity only reuses an
   identity that never delivered. The answer is host-side deduplication, and the chapter earns it
   by showing the plausible fix failing first.
6. **Ch. 37 — Enforcing architecture with tests.** The most portable idea in the book, and it
   applies to codebases that will never use this framework.
7. **Ch. 34 — Selection, and the brain that ignored it.** The best two-act production story in
   the material: a greeting web-searched six times the day narration made it visible; selection
   fixed what a run is *shown*; then a custom brain's side-channel proved shown is not *bound*,
   and enforcement closed it at the perimeter — spec first, both times, days apart. It also
   carries the observability counter-lesson: the audit trail that solved it was un-wired the
   same day it was connected, because the trace carries what users say, and some deployments
   promise never to read that.

---

## Tone

The framework's own documentation sets it, and the book should not drift from it:

- **Say the cost, not the feature.** "A budget that silently does not apply is worse than no budget,
  because it is claimed in the profile."
- **Name the failure that motivated the design.** Every rule here has a corpse behind it. Show it.
- **Admit what is unresolved.** The decorating-broker tension. The flattened sub-agent status. A book
  that admits its open questions is trusted on the closed ones.
- **No hedging and no marketing.** "Nineteen capabilities, three verbs each, and a test fails the
  build if one is missing" is a stronger sentence than any adjective available.

---

## Production order

Do **not** write the book first, and do not write it after. Write each part immediately after
shooting its season, while the failures are fresh and every listing has actually been run.

The video is the forcing function: **a chapter whose code was never filmed running is a chapter
whose code does not work.** That is exactly how the two vacuous conformance vectors got into this
project, and the book should be built so it cannot repeat it.

Ship parts I–III as a free sample. They are the part that earns the rest.
