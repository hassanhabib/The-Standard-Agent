# Support, Compatibility and Supply Chain

What you need in order to depend on `Standard.Agents` — thread safety, what is stable, how
deprecation works, and what ships with each release. SPEC.md §1.1 requires an implementation to
publish this; a conformance claim without it is a claim a consumer cannot act on.

---

## Readiness

The package claims a **readiness profile**, per version, and the claim is checkable by anyone:

```bash
dotnet run --project Standard.Agents.Conformance -- --profile Enterprise
```

Exit `0` means certified. It runs the same vectors CI runs, from a clone, with no access to
anything we hold privately.

| Profile | What an agent at this level has | Status |
|---|---|---|
| **Core** | conversation, skills, knowledge, memory, simple tools | certified |
| **Reliable** | guardians that see what they guard, durable decision log, run isolation, cancellation, timeouts | certified |
| **Enterprise** | identity-aware authorization, approval before irreversible acts, run-once effects, budgets, ranked retrieval | certified |
| **Critical** | conversation and effects that survive a process, compensation, native tool calls that round-trip, adversarial evaluation — a fooled Brain still cannot act outside the perimeter | certified |

---

## Thread safety and lifetime

**One `StandardAgent` instance is safe to use concurrently, and is the intended shape.** Register
it as a singleton; do not build one per request.

- Composition is guarded, so concurrent first calls build one graph rather than several.
- Run state — identity, counters, timing, guardian verdicts — is **per invocation, never per
  instance** (SPEC.md §4.4), so two prompts in flight cannot corrupt each other's records.
- The trace and decision log serialize their own writes.
- **Verified**, not asserted: 64 concurrent prompts on one instance with both sinks configured, in
  `DecisionLogTests`, plus conformance vector `12-concurrent-runs-are-isolated`.

Builder methods are **not** safe to call while prompts are in flight. Configure the agent, then
serve with it. Calling a builder method mid-flight invalidates the cached composition and races
against readers of it.

`StandardAgent` holds no unmanaged resources and does not need disposing. Brokers you supply are
yours to manage.

---

## What is stable

| Surface | Stability |
|---|---|
| `StandardAgent` builder methods | **Stable.** Removed only through deprecation (below). |
| `IAgent` | **Stable.** |
| Broker interfaces (`IGeneratorBroker`, `ISkillBroker`, …) | **Stable within a major version.** These are what provider packages implement, so a change here is a change to everyone's packages. |
| `AgentContext`, `AgentStatus`, and the models in `Models/` | **Stable within a major version.** New fields may be added; existing ones are not repurposed. |
| Service classes (`*Service`) and their constructors | **Not a public contract.** They are composed for you by `Compose()`; construct them directly at your own risk. |
| Anything under `Prompts/` | **Data, not API.** The built-in rubrics may be reworded in any release. Supply your own via `.Constitution(...)` / `.Consumption(...)` if you need them fixed. |

Version segments say what kind of change happened — `model . service/routine . fix/config . build`
— so a model change can never hide in the service segment. A change to a broker interface will
always move segment 1 or 2, never 3.

---

## Deprecation

Nothing is removed without warning:

1. The replacement ships first, alongside the old member.
2. The old member is marked `[Obsolete]` with a message naming the replacement, so you find out at
   **compile time**, not at runtime.
3. It keeps working, and its behavior stays pinned by a test, for at least **one minor version**.
4. Only then may it be removed, in a release whose segment reflects it.

The live example: `.LocalBrain` / `.LocalGate` / `.LocalJudge` became `.OnBrain` / `.OnGate` /
`.OnJudge` in 0.18.0.0 — a delegate you write is the *Custom* mode, not the *Local* one. The old
names still work and are held to the new ones by
`ShouldKeepObsoleteLocalAliasesBehavingLikeTheCustomVerbsAsync`.

A second live example, from 1.1.0.0: `AgentEffect.Principal` is the principal's identifier and
`AgentEffect.Identity` is the whole principal — tenant, jurisdiction, delegation. Both are set
together and cannot disagree. `Principal` is **not** deprecated: it is what a policy that only cares
who is acting should read. If it ever retires it will be at a major version, through the window
above.

**The V0 generator contract is not deprecated**, despite V1 arriving beside it. V0 is the text
protocol, and it is the one that works against any endpoint — including the small local models
that follow a format more reliably than they emit well-formed tool JSON. Nothing above is running
against it, and provider packages written for it need no change. See
[`docs/generator-contracts.md`](generator-contracts.md) for which to use and how to move.

---

## Upgrading

- Read the release notes; each release states what kind of change it was and why.
- Build with warnings visible. Deprecations arrive as compiler warnings naming the replacement,
  which is the cheapest possible upgrade signal.
- Re-run certification for the profile you depend on. It is the same command as above, and it
  answers the only question that matters: does the level I relied on still hold?

**Rolling back** is a version pin. Packages are never unlisted or overwritten, so an older version
remains installable; provider packages are versioned independently and are not required to move
with the core.

---

## Supply chain

Every change on `main` and every pull request runs:

- **Build with warnings as errors**, on the SDK pinned in `global.json` — the same compiler
  locally and in CI, because a version named in two places drifts in one of them.
- **The full unit suite and the conformance vectors**, plus certification of the claimed profile.
- **Dependency vulnerability audit**, transitively, failing the build on a finding.
- **Dependency licence listing**, transitively.
- **Secret scanning** over the diff.

Every release additionally publishes:

- A **software bill of materials** (CycloneDX JSON), generated at release time so it describes the
  artifact that shipped.
- **Signed build provenance** — a verifiable statement of which workflow, at which commit,
  produced those exact bytes.
- **Symbols and embedded sources** (`snupkg`), so you can step into a failure here rather than
  guess at it from a stack trace.

### What is not there

**The package is not Authenticode-signed.** That needs a code-signing certificate, which is an
organisational decision and a purchase, not a build setting. Provenance attestation covers the
"did this come from that source, unmodified" question; it does not cover "is the publisher who
they say they are." If your procurement requires a signed binary, that is the gap, and it is a
certificate away rather than a code change.

**The licence is TSSL, not an SPDX identifier.** Automated licence scanners will flag it as
unrecognised rather than as a problem. The full text is in `LICENSE.txt`; expect to route it
through legal review rather than through a scanner allow-list.
