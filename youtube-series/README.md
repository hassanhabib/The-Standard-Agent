# The Standard for Agents — the video series

**66 episodes across nine parts — ~14h 45m of content.** At a ~10 minute target that cuts to
**~109 videos.** From deciding whether to build one at all, to a five-agent system moving real
money for a regulated enterprise.

The same material is also a book — a different deliverable, not the scripts reformatted. See
[`BOOK.md`](BOOK.md).

This folder is the plan, in the open — the same way the spec precedes the code, the plan
precedes the recordings. Episode branches (`series/s{season}e{episode}-{slug}`) land as their
seasons are shot.

---

## The non-negotiable

**Every capability is shown all three ways — Local, External, Custom — in the episode that
introduces it.** Demonstrated and run, not named and tabled.

This is the framework's central claim: nineteen capabilities, three verbs each, and a build that
fails if one is missing. A series that shows only the Local mode has taught the easy third and
withheld the reason anyone would choose this over an afternoon of glue code. (The loop
capabilities — narration, the streamed outcome, selection and its enforcement — are deliberately
not triads; the matrix says why, and 7.9–7.10 own them.)

See **[`capability-matrix.md`](capability-matrix.md)** — the canonical list with exact signatures,
which episode owns each, and the two documented gaps.

It costs roughly **3 minutes per capability** and adds ~45 minutes across the series. It is the
best-spent time in the whole plan: it's the difference between "here's a memory feature" and
"here's a memory *seam*, and your Redis cluster drops into it."

---

## The spine

The framework has an arc built into it, and the series rides that rather than inventing one. An
agent is **Data · Decision · Direction** turning in a loop at every size — a ten-second one-liner
and a bank's compliance agent are the *same shape*. Every episode adds one opt-in line to natures
that are already there.

| Part | Eps | Videos | Runtime | Profile | The promise |
|---|---|---|---|---|---|
| **0 · From an Idea** | 5 | 6 | 58m | — | You know whether to build one, and exactly what. |
| **1 · Your First Agent** | 7 | 7 | 57m | — | It talks. Remote or local, one line apart. |
| **2 · Giving It Substance** | 7 | 11 | 95m | Core | It knows who it is and what it has read. |
| **3 · Giving It a Conscience** | 6 | 9 | 79m | Reliable | It can refuse, and explain itself. |
| **4 · The Enterprise Perimeter** | 8 | 14 | 117m | Enterprise | It can be trusted with someone else's money. |
| **5 · Mission Critical** | 7 | 14 | 113m | Critical | It survives the process it started in. |
| **6 · Multi-Agent Systems** | 7 | 15 | 113m | Critical | Five agents, one customer, real money. |
| **7 · Triggering It, and Running It** | 10 | 17 | 137m | — | It starts the right way, speaks while it works, and you are on call for it. |
| **8 · The Architecture** | 9 | 14 | 115m | — | For porting, extending, or judging it. |

Parts 0–7 are for people **using** the framework. Part 8 is for people **building on or against**
it — a different audience, and worth its own playlist.

---

## Why this order and not the obvious one

Most framework series front-load architecture: here are the tiers, here are the brokers, now let's
build something. That fails on YouTube because episode 1 has to earn episode 2, and nobody earns
attention with a layer diagram.

So: **a talking agent in 1.2, and the word "broker" is not spoken until part 8.** Every
architectural idea arrives the moment a viewer has a problem it solves. Foundations appear when a
failure gets misattributed. The 2–3 rule appears when a service has grown six dependencies. The
diagram is the *last* thing, not the first, and by then it's a recap rather than a lecture.

Part 0 is the other deliberate exception: it spends five episodes before any real code, because the
most expensive agent mistakes are made before anyone opens an editor. The other is 1.1, which sets `Agent = Orchestration(Data, Decision, Direction)` and nothing
else. That equation is the throughline — every later episode points back at which of the three it
just changed.

**Part 6 is the destination.** Everything before it builds one agent; a system that serves an
enterprise is several, and the framework's nesting is real but nothing propagates across it — not
budget, not identity, not run-once scope. That is seven episodes of engineering, not a footnote.

**Part 7 exists because every episode before it assumed something called `ProcessPromptAsync`.**
The framework has no trigger abstraction and correctly should not — a trigger is the exposer tier.
But the trigger KIND decides which capabilities stop being optional: whether a human can approve
in-session, whether identity exists at all, whether a budget is advisory, and whether run-once
actually protects you. Plus the operational half: thread safety, hosting, cancellation, testing, and
the four questions an architecture review will ask.

---

## Episode format

1. **Cold open (0:00–0:20)** — the problem, stated as a failure. Not "today we'll look at memory"
   but "restart this agent and it forgets your name. Here's the one line that fixes it."
2. **The build (bulk)** — screen, editor, real terminal. Code is typed or pasted in full and *run*.
   Never a slide of code that isn't executed on camera.
3. **All three modes** — Local, External, Custom, each run. Close with *nothing else about the agent
   changed*, and diff the file where it's short enough to fit.
4. **The gotcha (~1 min)** — the thing that will actually bite them. Every episode has one; each is
   real, taken from the docs or the code.
5. **What changed in the shape (~30s)** — which nature was touched, and what stayed identical.
6. **Recap + next (~20s)** — one sentence each.

**Hard rules for the recordings**

- Every snippet runs. If it needs an API key, show the key coming from config, never on screen.
- Never edit out a failure a viewer will hit. Show it, then fix it.
- Delete a line at the end of any episode that added one, and show the agent still running. That
  demonstrates the collapsible substrate better than any sentence about it.
- Terminal and editor ≥ 16pt. People watch this on phones.

---

## Splitting to ~10 minutes

64 episodes → ~104 videos. Splitting doesn't preserve runtime: each new video pays a cold open,
recap and outro tax of ~1.5–2 min, so a 14-minute episode becomes two 9-minute videos, not two
7-minute ones. Budget ~15h 30m of finished content.

Most splits have an obvious seam already in the plan — the three-mode block is frequently the
natural cut point, since "here's the capability" and "here are the three ways to back it" are two
different videos.

**Hold ~10 min as a target for parts 0–4 and let 5–8 run to 13–15.** By then the audience is
self-selected, and hard-splitting 5.4 mid-demo costs you the thing people came for: killing the
process and watching it resume, uncut.

---

## Companion repo

One branch per episode: `series/s{season}e{episode}-{slug}`, e.g. `series/s2e4-memory`. Each branch
is the finished state of that episode, so a viewer can `git checkout` and land exactly where the
video ends.

`Standard.Agents.Demo/` is the starting point for part 1 and the running example through part 4.
Part 5 needs its own sample with a genuinely irreversible tool (5.7), and part 6 needs the five-agent
system (6.7) — build that once and it carries parts 6 and 7.

---

## Titles, thumbnails, publishing

Titles state the capability and the payoff, not the episode number:

- ✅ "Your AI agent forgets everything on restart. One line fixes it."
- ❌ "Episode 11: Memory"

Season number stays in the playlist, out of the title. Thumbnails: one line of real code, large, on
the repo's dark palette (`#0a0d14` background, `#e6edf3` text) — the same colours as the
architecture diagram, so the series and the docs look like one thing.

Weekly, one season at a time, **with the whole season shot before the first episode ships.** Series
like this die when episode 4 takes three weeks; batching a season is what prevents it. Part 8 can
run in parallel on a second playlist — the audiences barely overlap.

---

## The files

- [`capability-matrix.md`](capability-matrix.md) — **read first**, it governs every episode
- [`BOOK.md`](BOOK.md) — the written edition: parts, chapters, appendices, and what differs
- [`season-0-from-an-idea.md`](season-0-from-an-idea.md)
- [`season-1-your-first-agent.md`](season-1-your-first-agent.md)
- [`season-2-giving-it-substance.md`](season-2-giving-it-substance.md)
- [`season-3-giving-it-a-conscience.md`](season-3-giving-it-a-conscience.md)
- [`season-4-the-enterprise-perimeter.md`](season-4-the-enterprise-perimeter.md)
- [`season-5-mission-critical.md`](season-5-mission-critical.md)
- [`season-6-multi-agent-systems.md`](season-6-multi-agent-systems.md)
- [`season-7-triggering-and-running-it.md`](season-7-triggering-and-running-it.md)
- [`season-8-the-architecture.md`](season-8-the-architecture.md)
