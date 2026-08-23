# Conformance Suite

A **language-neutral** set of behavioral test vectors that any Standard-Agents
implementation runs to self-certify against
[`SPEC.md`](https://github.com/hassanhabib/The-Standard-Agent-Specs/blob/main/SPEC.md).
Thirty-five vectors, four readiness profiles, every vector proven able to fail.

The vectors are the executable half of the specification. Prose can be read two ways; a vector
cannot, which is why "conformant" here means *this suite passes* rather than *we believe we
followed the document*.

## Why scripted

Agent behavior involves an LLM, which is non-deterministic — so it cannot be asserted
directly. Conformance instead tests the **deterministic** contracts: the loop, reply
interpretation, tool routing, the perimeter, and the feed-back of results into Data. It
does this by replacing the Brain with a **scripted generator** and tools with **stubs**,
then asserting on things that are stable cross-language contracts — the returned result
first among them, and, where a control is only observable from inside, what each broker
was actually handed.

That second kind matters more than it sounds. Several guarantees here are invisible from
the outside: an implementation that authorizes without a principal returns exactly the
same answer as one that does it properly. Certifying only the result would pass both.

## Vector schema (JSON)

```json
{
  "name": "tool-then-final",
  "description": "human-readable intent",
  "generatorReplies": ["ACTION: calculator: 1+1", "FINAL: 2"],
  "tools": { "calculator": "2" },
  "prompt": "what is 1+1",
  "expect": { "result": "2" }
}
```

- **generatorReplies** — the scripted Brain returns these in order, **repeating the last
  when exhausted** (so a single non-terminal reply exercises the turn cap).
- **tools** — stub *internal* tools: `name → fixed output`.
- **prompt** — the user task.
- **expect** — `{ "result": "<exact>" }` or `{ "resultContains": "<substring>" }`.

Those five are the whole schema for a Core vector, and the first seven vectors use nothing else.
Everything below is **optional and additive**: a field a vector omits behaves exactly as if the
capability it configures did not exist, which is why the early vectors still deserialize unchanged.

### Setup — what the agent is configured with

| Field | Effect |
|---|---|
| `prompts`, `concurrent` | drive several prompts through one agent, in order or all at once |
| `maxTurns` | cap the loop |
| `constitution`, `consumption` | inline markdown, written to a file the real builder is pointed at |
| `gateVerdict`, `judgeScore`, `gateVerdictOnToolOutput` | scripted guardian answers (default `allow` / `1.0`) |
| `redact` | boundary redaction |
| `requireApproval`, `approvalDecisions` | acts needing an authority, and its answers in order |
| `principal`, `deniedForPrincipal` | who is acting, and what a policy refuses them |
| `screenToolOutput` | screen untrusted tool results through the Gate |
| `cancelBeforeStart`, `transientFailures`, `retries`, `budgetMaxWallClockSeconds` | resilience and budget |
| `fallbackReply`, `failuresBeforeOpen` | the circuit breaker's alternative |
| `knowledge`, `knowledgeMaxResults` | real files in a real folder, so ranking is exercised |
| `sessionId` | run every prompt in one conversation |
| `compensateOnFailure`, `compensatingTools` | unwind a failed run; tools left out declare no way back |
| `newInstancePerPrompt`, `durableEffectLedger` | a fresh agent per prompt over shared folders — the harness's stand-in for a different process |
| `nativeReplies` | the Brain is a native (§6.2) generator returning structured choices |
| `allowTools`, `permissionMode` | the allow-list (`"tool"` or `"tool:scopePrefix"`) and the disposition toward acts nothing permitted |
| `toolRisk`, `toolScopeFirstWord` | what a stub tool declares about itself: how consequential it is, and what it is about to touch |

### Expectations — what must be true afterwards

| Field | Asserts |
|---|---|
| `result`, `resultContains` | the answer |
| `toolInput`, `toolRunCount`, `toolNeverRan` | what each tool was called with, and how often |
| `brainSees`, `brainNeverSees`, `noModelSees` | what reached the Brain, and what reached no model at all |
| `guardianRubricContains`, `guardianRubricExcludes` | the composed rubric, in **both** guardians |
| `judgeSawTask`, `guardianNeverAnswers`, `gateScreenedPromptTimes` | guardian integrity |
| `auditRunCount`, `auditRetainsEveryPrompt`, `auditSequencesUniquePerRun` | the decision log |
| `compensationOrder` | the exact order the unwind ran in |
| `toolResultAnswersCall` | the call id was replayed and answered |
| `policySawPrincipal` | the identity the policy broker was **handed when it decided** |

Two of those are worth singling out, because they are the difference between checking a control and
checking the *report* of one. `policySawPrincipal` reads the decision's input rather than the audit
log — an implementation that names the caller afterwards and authorizes without them fails it.
`noModelSees` covers every model call, not just the Brain's, because redaction that covers one call
and not the others is not redaction.

## Runner contract

To certify an implementation, provide a harness that, for each vector:

1. Wires a **scripted GeneratorBroker** returning `generatorReplies` in order (repeat the
   last when exhausted).
2. Registers `tools` as **internal** stub tools (each returns its fixed output).
3. Uses pass-through / stub brokers for everything else: Skill returns any text; Memory
   and Knowledge empty; Gate allows; Judge returns 1.0; External reports "not configured";
   Log is a no-op.
4. Runs the agent on `prompt` and compares the returned result to `expect`.

A conformant implementation **passes every vector**.

Two rules the reference harness follows, both learned the hard way:

- **Every double replaces a broker, never a service.** The whole 1·3·6·14 under test is the real
  library. A harness that stubs a service is certifying its own stub.
- **Observe through the real seams.** The decision log is watched through its own Custom sink, the
  generator is *wrapped* rather than replaced so redaction and streaming still run, and guardians
  answer through the real rubric composition. Anything observed off to the side is a claim about
  the harness rather than about the implementation.

## Reference runner (C#)

```bash
dotnet run --project Standard.Agents.Conformance
```

Exit code `0` = all vectors pass. It reads the vectors from this folder and runs them
against the `Standard.Agents` reference library. Use it as the template for a runner in
your own language.

## Adding a language

Implement the harness (steps 1–4 above) in your language, point it at
`conformance/vectors/`, and run. If every vector passes, your implementation conforms to
the deterministic core of the Standard.

## What these vectors cover

| Vector | Verifies |
|---|---|
| `direct-answer` | A `FINAL` returns in one turn |
| `tool-then-final` | A tool result feeds back into Data and is used next turn |
| `first-line-action-only` | Only the first line's `ACTION` is parsed; extra lines ignored |
| `multiline-final` | A `FINAL` answer may span multiple lines |
| `unknown-tool-recovers` | An unknown tool routes to External and the agent recovers |
| `max-turns-cap` | A never-terminating Brain is capped by the loop |
| `structured-tool-call` | A structured `TOOL:` call (§6.1) routes to the tool with its arguments |
| `gate-refusal-short-circuits` | A Gate refusal ends the run without reaching the Brain |
| `constitution-binds-guardians` | A constitution reaches *both* guardian rubrics |
| `consumption-replaces-policy` | A consumption skill replaces the policy in both, contract intact |
| `audit-retains-every-run` | Beginning a run never discards a prior run's records (§4.7) |
| `concurrent-runs-are-isolated` | Eight prompts at once on one instance; no record corrupts another |
| `judge-receives-the-task` | The Judge scores against the task, not the candidate alone (§4.2) |
| `redaction-covers-every-model-call` | Brain, Gate and Judge all see the token, never the value (§4.6) |
| `guardian-overreach-is-neutralized` | A guardian that answers or acts is classified, not obeyed (§7.6) |
| `approval-blocks-irreversible-tool` | `Pending` holds the act — waiting is not consent (§4.9) |
| `duplicate-effect-executes-once` | One act however many times it is proposed; the key is canonical |
| `injected-instruction-in-tool-output-is-refused` | Indirect injection is withheld from the Brain (§4.9) |
| `cancellation-stops-the-loop` | A cancelled run stops at a turn boundary and is not an answer |
| `transient-failure-recovers` | Retry by error category, not by matching a message (§4.10) |
| `budget-stops-the-loop` | Exhaustion stops the loop and is distinguishable from a refusal |
| `budget-bounds-tokens-on-any-protocol` | A token bound holds where the provider reports no usage (§4.10) |
| `budget-bounds-cost-on-any-protocol` | A cost bound holds there too — cost is priced off the count |
| `a-repeat-in-a-session-is-a-new-act` | Run-once is scoped to a run; a repeat in a later run performs (§4.9) |
| `an-allow-list-can-say-where` | Permission is what **and where**; the tool names the scope (§4.9) |
| `ask-first-covers-what-nothing-permitted` | A mode answers for the acts no permission mentioned (§4.9) |
| `knowledge-retrieves-by-relevance` | Retrieval returns the passage that answers, not the first found |
| `guardian-screens-once-per-prompt` | An unchanged prompt is screened once, not once per turn |
| `open-circuit-falls-back-to-secondary` | An unhealthy provider degrades rather than fails (§4.10) |
| `conversation-carries-history` | A follow-up resolves against what came before (§4.11) |
| `failed-run-unwinds-in-reverse` | Compensation undoes what was performed, newest first (§4.9) |
| `effect-outcome-survives-a-crash` | A new instance resumes and does not repeat the act (§4.9, §4.11) |
| `awaiting-approval-resumes-in-a-new-process` | A held act runs once the authority says yes, elsewhere |
| `native-tool-call-round-trips` | A result returns as a tool message naming its call (§6.2) |
| `policy-authorizes-on-identity` | The principal reaches the decision, not only the log (§3.3, §4.9) |

## Readiness profiles

Full (§8.2) spans an agent with a Judge and an agent that can move money, so the vectors are also
grouped into levels. Each is a list of required vector names in `conformance/profiles/`, which is
what makes a claim answerable with an exit code rather than an opinion (SPEC.md §1.1):

```bash
dotnet run --project Standard.Agents.Conformance -- --profile Critical
```

| Profile | Adds |
|---|---|
| **Core** | conversation, skills, knowledge, memory, simple tools |
| **Reliable** | guardians that see what they guard, a durable decision log, run isolation, cancellation, timeouts |
| **Enterprise** | identity-aware authorization, approval before irreversible acts, run-once, budgets, ranked retrieval |
| **Critical** | conversation and effects that survive a process, compensation, native tool calls that round-trip |

A profile names the evidence it requires **before** that evidence exists — the level is the target,
not a description of what already passes. `NOT CERTIFIED` lists exactly which vectors are missing.

## Sabotage-verification

A vector that cannot fail proves nothing, and vectors written from a passing implementation are
prone to exactly that. Every vector here has been **proven able to fail**: the behaviour it checks
was deliberately broken, the vector observed failing, and the break reverted.

Two in this suite were vacuous when first written and were caught this way —
`knowledge-retrieves-by-relevance` passed with the ranking inverted, and
`redaction-covers-every-model-call` passed while only the Brain's input was being recorded. Both
were rewritten. If you add a vector, break the thing it covers first.

The sharper lesson is what a **missing** vector costs, because nothing here goes red for one.
Until `1.4.0` the only budget a vector could express was wall-clock, so the token and cost bounds
were certified by nothing at all. This implementation reported no usage on the text protocol,
enforced neither bound, and passed every vector and every profile — Enterprise included, which
claims budgets by name — for eight releases. Anyone building to these vectors in another language
would have reproduced that exactly and been entitled to the same badge. A suite that cannot
express a bound cannot certify it, and silence reads as coverage.
